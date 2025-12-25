// SND_ManeuverThrustersHandler.cs — финальная версия с плавным затуханием 0.2 сек
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Data.Audio;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.Utils;
using VRageMath;
using System;
using System.Collections.Generic;
using static SEENG_ES.SpeedManager;
using VRage.ModAPI;

namespace SEENG_ES
{
    public static class SND_ManeuverThrustersHandler
    {
        public class Thruster
        {
            public MyEntity3DSoundEmitter Emitter;
            public Vector3 Offset;
            public float CooldownTimer = 0f;
            public bool WasActive = false;
            public bool IsFading = false;        
            public float FadeTimer = 0f;         
        }

        public static readonly List<Thruster> _thrusters = new List<Thruster>(6);
        private static readonly Random _rnd = new Random();
        private static string _currentCue = "";
        private const float MAX_SPEED_THRESHOLD = 0.15f; // max speed limit to play

        private const float FADE_OUT_TIME = 0.20f; // ← 0.2 fade

        private static readonly Vector3[] OFFSETS = new Vector3[]
        {
            new Vector3( 0,   0, -20), // 0: BACK
            new Vector3( 0,   0,  20), // 1: FORWARD
            new Vector3(-20,  0,   0), // 2: LEFT
            new Vector3( 20,  0,   0), // 3: RIGHT
            new Vector3( 0,  20,   0), // 4: UP
            new Vector3( 0, -20,   0)  // 5: DOWN
        };

        public static void Restart(IMyCockpit cockpit, string prefix)
        {
            StopAll();

            if (cockpit == null) return;

            _currentCue = string.IsNullOrEmpty(prefix) ? "SeengManeuverThrusters" : $"SeengManeuverThrusters_{prefix}";
            var pair = new MySoundPair(_currentCue);

            var pos = cockpit.GetPosition();
            var wm = cockpit.WorldMatrix;

            for (int i = 0; i < 6; i++)
            {
                var worldOffset = Vector3.TransformNormal(OFFSETS[i], wm);
                var emitter = new MyEntity3DSoundEmitter((MyEntity)(IMyEntity)cockpit, false, 1f);
                emitter.Force3D = true;
                emitter.SetPosition(pos + worldOffset);

                _thrusters.Add(new Thruster
                {
                    Emitter = emitter,
                    Offset = OFFSETS[i],
                    CooldownTimer = (float)_rnd.NextDouble() * 1.6f + 0.4f
                });
            }
        }

        public static void Update(IMyCockpit cockpit, RotationManager rotationManager, SpeedManager speedManager)
        {
            if (cockpit == null || rotationManager == null || _thrusters.Count == 0) return;
            float normalizedSpeed = speedManager.NormalizedSpeed;
            bool allowManeuver = normalizedSpeed <= MAX_SPEED_THRESHOLD;
            string debugText = $"Angular Speed: {rotationManager.AngularSpeedRad:F3} rad/s ({rotationManager.AngularSpeedDeg:F1}°/s)\n" +
                       $"Roll: {rotationManager.RollRate:+0.00;-0.00;0.00} | " +
                       $"Pitch: {rotationManager.PitchRate:+0.00;-0.00;0.00} | " +
                       $"Yaw: {rotationManager.YawRate:+0.00;-0.00;0.00}\n" +
                       $"Maneuver Thrusters: {(allowManeuver ? "ACTIVE" : "INACTIVE (>15% speed)")}";

            ///MyAPIGateway.Utilities.ShowNotification(debugText, 16, allowManeuver ? "White" : "Red");
            if (!allowManeuver)
            {
                foreach (var t in _thrusters)
                {
                    if (t.Emitter?.IsPlaying == true)
                    {
                        t.Emitter.Sound.VolumeMultiplier = 0f;
                        t.Emitter.StopSound(true);
                    }
                    t.WasActive = false;
                    t.IsFading = false;
                }
                return;
            }

            Matrix inv = Matrix.Transpose(cockpit.WorldMatrix);
            Vector3 rel = Vector3.TransformNormal(rotationManager.AngularVelocity, inv);

            float yaw = rel.Z;
            float pitch = rel.Y;
            float roll = rel.X;

            float dt = 1f / 60f;

            UpdateThruster(0, pitch < -0.12f, dt); // BACK
            UpdateThruster(1, pitch > 0.12f, dt); // FORWARD
            UpdateThruster(2, yaw < -0.12f, dt); // LEFT
            UpdateThruster(3, yaw > 0.12f, dt); // RIGHT
            UpdateThruster(4, roll > 0.12f, dt); // UP
            UpdateThruster(5, roll < -0.12f, dt); // DOWN
        }

        private static void UpdateThruster(int index, bool shouldBeActive, float dt)
        {
            var t = _thrusters[index];

            if (shouldBeActive)
            {
                t.IsFading = false;
                t.FadeTimer = 0f;

                t.CooldownTimer -= dt;
                if (t.CooldownTimer <= 0f && (t.Emitter?.IsPlaying != true))
                {
                    t.Emitter?.PlaySound(new MySoundPair(_currentCue));
                    t.CooldownTimer = (float)_rnd.NextDouble() * 1.6f + 0.4f;
                }

                t.WasActive = true;
            }
            else
            {
                if (t.WasActive && !t.IsFading)
                {
                    t.IsFading = true;
                    t.FadeTimer = FADE_OUT_TIME;
                }

                if (t.IsFading)
                {
                    t.FadeTimer -= dt;
                    float volume = MathHelper.Clamp(t.FadeTimer / FADE_OUT_TIME, 0f, 1f);
                    if (t.Emitter?.Sound != null)
                        t.Emitter.Sound.VolumeMultiplier = volume;

                    if (t.FadeTimer <= 0f)
                    {
                        t.Emitter?.StopSound(true);
                        t.IsFading = false;
                    }
                }

                t.WasActive = false;
            }
        }

        public static void StopAll()
        {
            foreach (var t in _thrusters)
            {
                t.Emitter?.StopSound(true);
            }
            _thrusters.Clear();
        }
    }
}