using System;
using System.Diagnostics;
using Sandbox;
using Sandbox.ModAPI;
using VRage.Input;
using VRageMath;

namespace SEENG_ES
{
    public class ManagersUpdater
    {
        private readonly SpeedManager _speedManager;
        private readonly ThrustManager _thrustManager;

        public ManagersUpdater(SpeedManager speedManager, ThrustManager thrustManager)
        {
            _speedManager = speedManager ?? throw new ArgumentNullException(nameof(speedManager));
            _thrustManager = thrustManager ?? throw new ArgumentNullException(nameof(thrustManager));
        }
        public void Update(IMyCockpit cockpit)
        {
            _thrustManager.Update();
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

        public void Reset()
        {
            _thrustManager.Reset();
            _speedManager.SetNormalizedSpeed(0f);
        }
    }
    public class ThrustManager
    {
        public bool IsThrusting { get; private set; } = false;
        public readonly Stopwatch DecayStartTime = new Stopwatch();

        public void Update()
        {
            if (MyAPIGateway.Input == null) return; // this shit is temporary
            bool forward = MyAPIGateway.Input.IsKeyPress(MyKeys.W);
            bool back = MyAPIGateway.Input.IsKeyPress(MyKeys.S);
            bool left = MyAPIGateway.Input.IsKeyPress(MyKeys.A);
            bool right = MyAPIGateway.Input.IsKeyPress(MyKeys.D);
            bool up = MyAPIGateway.Input.IsKeyPress(MyKeys.Space);
            bool down = MyAPIGateway.Input.IsKeyPress(MyKeys.C);

            bool anyThrust = forward || back || left || right || up || down;

            if (anyThrust && !IsThrusting)
            {
                IsThrusting = true;
            }
            else if (!anyThrust && IsThrusting)
            {
                IsThrusting = false;
            }
        }

        public void StartDecay()
        {
            DecayStartTime.Restart();
        }

        public void Reset()
        {
            IsThrusting = false;
            DecayStartTime.Reset();
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