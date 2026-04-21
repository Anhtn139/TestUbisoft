using System;
using System.Collections.Generic;
using TestUbisoft.Prototype.Core;
using TestUbisoft.Prototype.Messaging;

namespace TestUbisoft.Prototype.Client
{
    /// <summary>
    /// Client orchestration layer.
    /// This layer translates local input into messages and interpolates authoritative snapshots for presentation.
    /// </summary>
    public sealed class ClientGame : IClientGame
    {
        private const int MaxBufferedSnapshots = 32;

        private readonly List<WorldSnapshot> _snapshots = new List<WorldSnapshot>(MaxBufferedSnapshots);
        private readonly List<EntitySnapshot> _interpolatedEntities = new List<EntitySnapshot>(128);
        private readonly List<EntitySnapshot> _renderEntities = new List<EntitySnapshot>(128);
        private readonly Dictionary<int, EntitySnapshot> _toEntitiesById = new Dictionary<int, EntitySnapshot>(128);

        private GameConfig _config;
        private IMessageTransport _transport;
        private IClientWorldView _worldView;
        private SimVector2 _currentMoveInput;
        private SimVector2 _lastSentMoveInput;
        private SimVector2 _predictedLocalPosition;
        private SimVector2 _latestAuthoritativeLocalPosition;
        private float _predictedLocalYawDegrees;
        private float _latestAuthoritativeLocalYawDegrees;
        private int _inputSequence;
        private bool _isInitialized;
        private bool _hasPredictedLocalPose;
        private bool _hasAuthoritativeLocalPose;

        public void Initialize(GameConfig config, IMessageTransport transport, IClientWorldView worldView)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _transport = transport ?? throw new ArgumentNullException(nameof(transport));
            _worldView = worldView ?? throw new ArgumentNullException(nameof(worldView));

            _snapshots.Clear();
            _currentMoveInput = SimVector2.Zero;
            _lastSentMoveInput = SimVector2.Zero;
            _predictedLocalPosition = SimVector2.Zero;
            _latestAuthoritativeLocalPosition = SimVector2.Zero;
            _predictedLocalYawDegrees = 0f;
            _latestAuthoritativeLocalYawDegrees = 0f;
            _inputSequence = 0;
            _isInitialized = true;
            _hasPredictedLocalPose = false;
            _hasAuthoritativeLocalPose = false;

            SendMoveInput(SimVector2.Zero);
            DrainSnapshots();
        }

        public void SetLocalMoveInput(SimVector2 moveInput)
        {
            if (!_isInitialized)
            {
                return;
            }

            SimVector2 clampedInput = SimVector2.ClampMagnitude(moveInput, 1f);
            _currentMoveInput = clampedInput;
            if (clampedInput.Equals(_lastSentMoveInput))
            {
                return;
            }

            SendMoveInput(clampedInput);
        }

        public void Tick(float deltaSeconds, double clientTime)
        {
            if (!_isInitialized)
            {
                return;
            }

            AdvanceLocalPrediction(deltaSeconds);
            DrainSnapshots();
            RenderInterpolatedSnapshot(clientTime - _config.SnapshotInterpolationDelay);
        }

        public void Shutdown()
        {
            _worldView?.Clear();
            _snapshots.Clear();
            _worldView = null;
            _transport = null;
            _config = null;
            _isInitialized = false;
        }

        private void AdvanceLocalPrediction(float deltaSeconds)
        {
            if (!_hasPredictedLocalPose)
            {
                if (!_hasAuthoritativeLocalPose)
                {
                    return;
                }

                _predictedLocalPosition = _latestAuthoritativeLocalPosition;
                _predictedLocalYawDegrees = _latestAuthoritativeLocalYawDegrees;
                _hasPredictedLocalPose = true;
            }

            if (_hasAuthoritativeLocalPose)
            {
                float correctionDistance = _config.PlayerMoveSpeed * 10f * deltaSeconds;
                _predictedLocalPosition = MoveTowards(
                    _predictedLocalPosition,
                    _latestAuthoritativeLocalPosition,
                    correctionDistance);
            }

            if (_currentMoveInput.SqrMagnitude <= 0.0001f)
            {
                if (_hasAuthoritativeLocalPose)
                {
                    float correctionT = Clamp01(deltaSeconds * 12f);
                    _predictedLocalYawDegrees = LerpAngle(
                        _predictedLocalYawDegrees,
                        _latestAuthoritativeLocalYawDegrees,
                        correctionT);
                }

                return;
            }

            _predictedLocalPosition += _currentMoveInput * (_config.PlayerMoveSpeed * deltaSeconds);
            _predictedLocalYawDegrees = CalculateYawDegrees(_currentMoveInput);

            if (_hasAuthoritativeLocalPose)
            {
                _predictedLocalPosition = ClampDistance(
                    _predictedLocalPosition,
                    _latestAuthoritativeLocalPosition,
                    _config.PlayerMoveSpeed * GetMaxPredictionLeadSeconds());
            }
        }

        private void SendMoveInput(SimVector2 moveInput)
        {
            _lastSentMoveInput = moveInput;
            _transport.SendToServer(new ClientInputMessage(_config.LocalClientId, _inputSequence++, moveInput));
        }

        private void DrainSnapshots()
        {
            while (_transport.TryDequeueForClient(out ServerSnapshotMessage message))
            {
                if (message.Snapshot == null)
                {
                    continue;
                }

                InsertSnapshotSorted(message.Snapshot);
                while (_snapshots.Count > MaxBufferedSnapshots)
                {
                    _snapshots.RemoveAt(0);
                }
            }
        }

        private void InsertSnapshotSorted(WorldSnapshot snapshot)
        {
            int insertIndex = _snapshots.Count;
            for (int i = 0; i < _snapshots.Count; i++)
            {
                if (snapshot.ServerTime < _snapshots[i].ServerTime)
                {
                    insertIndex = i;
                    break;
                }
            }

            _snapshots.Insert(insertIndex, snapshot);
        }

        private void RenderInterpolatedSnapshot(double renderTime)
        {
            if (_snapshots.Count == 0)
            {
                return;
            }

            WorldSnapshot from = _snapshots[0];
            WorldSnapshot to = _snapshots[_snapshots.Count - 1];

            for (int i = 0; i < _snapshots.Count - 1; i++)
            {
                if (_snapshots[i + 1].ServerTime < renderTime)
                {
                    continue;
                }

                from = _snapshots[i];
                to = _snapshots[i + 1];
                break;
            }

            RemoveConsumedSnapshots(renderTime);

            if (ReferenceEquals(from, to) || to.ServerTime <= from.ServerTime)
            {
                _worldView.Render(ApplyLocalPlayerPrediction(to));
                return;
            }

            double duration = to.ServerTime - from.ServerTime;
            float t = duration <= 0.0001 ? 1f : Clamp01((float)((renderTime - from.ServerTime) / duration));
            WorldSnapshot interpolated = CreateInterpolatedSnapshot(from, to, t, renderTime);
            _worldView.Render(ApplyLocalPlayerPrediction(interpolated));
        }

        private void RemoveConsumedSnapshots(double renderTime)
        {
            while (_snapshots.Count > 2 && _snapshots[1].ServerTime < renderTime)
            {
                _snapshots.RemoveAt(0);
            }
        }

        private WorldSnapshot CreateInterpolatedSnapshot(WorldSnapshot from, WorldSnapshot to, float t, double renderTime)
        {
            _interpolatedEntities.Clear();
            _toEntitiesById.Clear();

            for (int i = 0; i < to.Entities.Count; i++)
            {
                EntitySnapshot entity = to.Entities[i];
                _toEntitiesById[entity.EntityId] = entity;
            }

            for (int i = 0; i < from.Entities.Count; i++)
            {
                EntitySnapshot fromEntity = from.Entities[i];
                if (!_toEntitiesById.TryGetValue(fromEntity.EntityId, out EntitySnapshot toEntity))
                {
                    continue;
                }

                if (fromEntity.Kind == EntityKind.Egg)
                {
                    _interpolatedEntities.Add(toEntity);
                    continue;
                }

                SimVector2 position = Lerp(fromEntity.Position, toEntity.Position, t);
                float yaw = LerpAngle(fromEntity.YawDegrees, toEntity.YawDegrees, t);
                _interpolatedEntities.Add(new EntitySnapshot(
                    toEntity.EntityId,
                    toEntity.OwnerClientId,
                    toEntity.Kind,
                    position,
                    yaw,
                    toEntity.IsActive,
                    toEntity.VisualIndex));
            }

            return new WorldSnapshot(
                to.Tick,
                renderTime,
                to.RemainingSeconds,
                _interpolatedEntities,
                to.Scores);
        }

        private WorldSnapshot ApplyLocalPlayerPrediction(WorldSnapshot snapshot)
        {
            _renderEntities.Clear();
            var replacedLocalPlayer = false;

            for (int i = 0; i < snapshot.Entities.Count; i++)
            {
                EntitySnapshot entity = snapshot.Entities[i];
                if (entity.Kind == EntityKind.Player
                    && string.Equals(entity.OwnerClientId, _config.LocalClientId, StringComparison.Ordinal))
                {
                    _latestAuthoritativeLocalPosition = entity.Position;
                    _latestAuthoritativeLocalYawDegrees = entity.YawDegrees;
                    _hasAuthoritativeLocalPose = true;

                    if (!_hasPredictedLocalPose)
                    {
                        _predictedLocalPosition = entity.Position;
                        _predictedLocalYawDegrees = entity.YawDegrees;
                        _hasPredictedLocalPose = true;
                    }

                    _renderEntities.Add(new EntitySnapshot(
                        entity.EntityId,
                        entity.OwnerClientId,
                        entity.Kind,
                        _predictedLocalPosition,
                        _predictedLocalYawDegrees,
                        entity.IsActive,
                        entity.VisualIndex));
                    replacedLocalPlayer = true;
                    continue;
                }

                _renderEntities.Add(entity);
            }

            if (!replacedLocalPlayer)
            {
                return snapshot;
            }

            return new WorldSnapshot(
                snapshot.Tick,
                snapshot.ServerTime,
                snapshot.RemainingSeconds,
                _renderEntities,
                snapshot.Scores);
        }

        private static SimVector2 Lerp(SimVector2 from, SimVector2 to, float t)
        {
            return new SimVector2(
                from.X + (to.X - from.X) * t,
                from.Y + (to.Y - from.Y) * t);
        }

        private static float LerpAngle(float from, float to, float t)
        {
            float delta = Repeat((to - from) + 180f, 360f) - 180f;
            return from + delta * t;
        }

        private static float Clamp01(float value)
        {
            if (value < 0f)
            {
                return 0f;
            }

            return value > 1f ? 1f : value;
        }

        private static float Repeat(float value, float length)
        {
            return value - (float)Math.Floor(value / length) * length;
        }

        private float GetMaxPredictionLeadSeconds()
        {
            float latencyLead = _config.ClientToServerLatencyRange.Y + (_config.FixedDeltaSeconds * 2f);
            if (latencyLead < 0.05f)
            {
                return 0.05f;
            }

            return latencyLead > 0.15f ? 0.15f : latencyLead;
        }

        private static SimVector2 MoveTowards(SimVector2 current, SimVector2 target, float maxDistanceDelta)
        {
            SimVector2 delta = target - current;
            float distance = delta.Magnitude;
            if (distance <= maxDistanceDelta || distance <= 0.0001f)
            {
                return target;
            }

            return current + (delta * (maxDistanceDelta / distance));
        }

        private static SimVector2 ClampDistance(SimVector2 value, SimVector2 origin, float maxDistance)
        {
            SimVector2 delta = value - origin;
            float distance = delta.Magnitude;
            if (distance <= maxDistance || distance <= 0.0001f)
            {
                return value;
            }

            return origin + (delta * (maxDistance / distance));
        }

        private static float CalculateYawDegrees(SimVector2 moveInput)
        {
            return (float)(Math.Atan2(moveInput.X, moveInput.Y) * 180.0 / Math.PI);
        }
    }
}
