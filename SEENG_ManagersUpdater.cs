using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Sandbox;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Ingame;
using VRage.Game.ModAPI;
using VRage.Input;
using VRageMath;
using static SEENG_ES.SpeedManager;
using IMyCockpit = Sandbox.ModAPI.IMyCockpit;
using IMyThrust = Sandbox.ModAPI.IMyThrust;

namespace SEENG_ES
{
    public class ManagersUpdater
    {
        private readonly SpeedManager _speedManager;
        private readonly ThrustManager _thrustManager;
        private readonly RotationManager _rotationManager;

        public ManagersUpdater(SpeedManager speedManager, ThrustManager thrustManager)
        {
            _speedManager = speedManager ?? throw new ArgumentNullException(nameof(speedManager));
            _thrustManager = thrustManager ?? throw new ArgumentNullException(nameof(thrustManager));
            _rotationManager = new RotationManager();
        }

        public void Update(IMyCockpit cockpit)
        {
            _thrustManager.Update(cockpit);
            _rotationManager.Update(cockpit);
            if (cockpit != null)
            {
                _speedManager.Update(cockpit);
            }
            else
            {
                _speedManager.SetNormalizedSpeed(0f);
            }
        }

        public SpeedManager SpeedManager => _speedManager;
        public ThrustManager ThrustManager => _thrustManager;
        public RotationManager RotationManager => _rotationManager;

        public void Reset()
        {
            _thrustManager.Reset();
            _speedManager.SetNormalizedSpeed(0f);
        }
    }

    public class ThrustManager
    {
        public bool IsThrusting { get; private set; } = false;
        public bool IsPushActive { get; private set; } = false;
        public Vector3 ControlThrust { get; private set; } = Vector3.Zero;
        public readonly Stopwatch DecayStartTime = new Stopwatch();

        public IMyCockpit _currentCockpit;
        private readonly List<IMySlimBlock> _tempBlocks = new List<IMySlimBlock>();

        public void Update(IMyCockpit cockpit)
        {
            if (MyAPIGateway.Input == null) return;

            if (cockpit == null)
            {
                IsThrusting = false;
                IsPushActive = false;
                ControlThrust = Vector3.Zero;
                _currentCockpit = null;
                DecayStartTime.Reset();
                return;
            }

            if (_currentCockpit != cockpit)
            {
                _currentCockpit = cockpit;
            }

            var grid = cockpit.CubeGrid;
            var moveInd = cockpit.MoveIndicator;
            ControlThrust = moveInd;

            bool anyInput = moveInd.LengthSquared() > 0.01f;

            _tempBlocks.Clear();

            grid.GetBlocks(_tempBlocks, block =>
            {
                var thrust = block.FatBlock as IMyThrust;
                return thrust != null && thrust.IsFunctional && thrust.Enabled;
            });

            bool hasActiveThrusters = _tempBlocks.Count > 0;

            bool prevThrusting = IsThrusting;

            IsThrusting = anyInput && hasActiveThrusters;
            IsPushActive = anyInput && hasActiveThrusters;

            if (!IsThrusting && prevThrusting)
            {
                IsPushLooping = false;
            }
        }

        public bool IsPushLooping { get; set; } = false;

        public void StartDecay()
        {
            DecayStartTime.Restart();
        }

        public void Reset()
        {
            IsThrusting = false;
            IsPushActive = false;
            IsPushLooping = false;
            ControlThrust = Vector3.Zero;
            DecayStartTime.Reset();
            _currentCockpit = null;
        }
    }

    public class SpeedManager
    {
        public float MaxSpeed { get; set; }
        private Vector3 _lastVelocity = Vector3.Zero;
        private float _lastTime = 0f;
        private float _currentAcceleration = 0f;
        public float Acceleration => _currentAcceleration;
        public float NormalizedSpeed { get; private set; } = 0f;
        public float LastNormalizedSpeed { get; set; } = 0f;
        public readonly Stopwatch AccelerationStartTime = new Stopwatch();
        public readonly Stopwatch LastStartTime = new Stopwatch();
        public readonly Stopwatch IncreaseCheckTime = new Stopwatch();
        public float FadeDirection { get; set; } = 0f;

        public SpeedManager(float maxSpeed = 120f)
        {
            MaxSpeed = maxSpeed;
            LastStartTime.Start();
            IncreaseCheckTime.Start();
        }

        public class RotationManager
        {
            public Vector3 AngularVelocity { get; private set; } = Vector3.Zero;
            public float AngularSpeedRad { get; private set; } = 0f;
            public float AngularSpeedDeg { get; private set; } = 0f;
            public Vector3 AngularDirection { get; private set; } = Vector3.Zero;
            public Vector3 LocalAngularVelocity { get; private set; } = Vector3.Zero;

            public float RollRate => LocalAngularVelocity.X;
            public float PitchRate => LocalAngularVelocity.Y;
            public float YawRate => LocalAngularVelocity.Z;

            private IMyCockpit _lastCockpit;

            public void Update(IMyCockpit cockpit)
            {
                if (cockpit?.CubeGrid?.Physics == null)
                {
                    Reset();
                    return;
                }
                if (_lastCockpit != cockpit)
                {
                    _lastCockpit = cockpit;
                }

                var physics = cockpit.CubeGrid.Physics;

                AngularVelocity = physics.AngularVelocity;
                AngularSpeedRad = AngularVelocity.Length();
                AngularSpeedDeg = MathHelper.ToDegrees(AngularSpeedRad);
                if (AngularSpeedRad > 0.001f)
                    AngularDirection = Vector3.Normalize(AngularVelocity);
                else
                    AngularDirection = Vector3.Zero;

                Matrix worldMatrix = cockpit.CubeGrid.WorldMatrix;
                Vector3 forward = worldMatrix.Forward;
                Vector3 up = worldMatrix.Up;
                Vector3 right = worldMatrix.Right;

                float roll = Vector3.Dot(AngularVelocity, forward);
                float pitch = Vector3.Dot(AngularVelocity, right);   
                float yaw = Vector3.Dot(AngularVelocity, up);      

                LocalAngularVelocity = new Vector3(roll, pitch, yaw);
            }

            public void Reset()
            {
                AngularVelocity = Vector3.Zero;
                AngularSpeedRad = 0f;
                AngularSpeedDeg = 0f;
                AngularDirection = Vector3.Zero;
                LocalAngularVelocity = Vector3.Zero;
            }
        }
        public void Update(IMyCockpit cockpit)
        {
            if (cockpit?.CubeGrid?.Physics == null)
            {
                SetNormalizedSpeed(0f);
                return;
            }

            var grid = cockpit.CubeGrid;
            float currentTime = (float)(MySandboxGame.TotalGamePlayTimeInMilliseconds / 1000.0);
            if (_lastTime == 0f)
            {
                _lastTime = currentTime;
                _lastVelocity = grid.Physics.LinearVelocity;
                LastNormalizedSpeed = 0f;
                return;
            }

            float deltaT = currentTime - _lastTime;
            if (deltaT > 0f)
            {
                Vector3 currentVelocity = grid.Physics.LinearVelocity;
                Vector3 deltaVel = currentVelocity - _lastVelocity;
                _currentAcceleration = Vector3.Dot(deltaVel / deltaT, grid.WorldMatrix.Forward);

                if (Math.Abs(_currentAcceleration) > 0.1f)
                {
                    if (!AccelerationStartTime.IsRunning)
                    {
                        AccelerationStartTime.Restart();
                    }
                }
                else
                {
                    AccelerationStartTime.Reset();
                }
            }

            float speed = grid.Physics.LinearVelocity.Length();
            SetNormalizedSpeed(MathHelper.Clamp(speed / MaxSpeed, 0f, 1f));

            _lastVelocity = grid.Physics.LinearVelocity;
            _lastTime = currentTime;
        }

        public void SetNormalizedSpeed(float value)
        {
            NormalizedSpeed = MathHelper.Clamp(value, 0f, 1f);
            LastNormalizedSpeed = NormalizedSpeed;
        }
    }
}