using System.Collections.Generic;
using UnityEngine;

namespace LittlePaws.Rescue
{
    public enum RescueEnemyMode { Patrol, Chase, Damaged, Lying, Returning, Dead }

    public sealed class RescueEnemy
    {
        public Vector2 Position;
        public Vector2 PatrolOrigin;
        public int Health = 30;
        public int Facing = -1;
        public RescueEnemyMode Mode;
        public float ModeTime;
        public bool HealthVisible;
        public float AttackCooldown = 1f;
        public int AttackSerial;
        public float AttackAge = 10f;
        internal bool AttackPending;
        internal int KnockbackDirection;
        public bool IsVulnerable { get { return Health > 0 && Mode != RescueEnemyMode.Lying; } }
    }

    // Independent of rendering and input devices so combat can be checked deterministically.
    public sealed class RescueCombat
    {
        public const float StageLength = 15f;
        public const float HitRange = 1f;
        public const float DetectionRange = 5f;
        public const float DamageDuration = .5f;
        public const float LyingDuration = 2f;
        public const float HeroSpeed = 3f;
        public const float EnemyAttackWindup = .2f;
        public readonly List<RescueEnemy> Enemies = new List<RescueEnemy>();
        public Vector2 HeroPosition { get; set; }
        public int HeroHealth { get; private set; } = 50;
        public int Facing { get; private set; } = 1;
        public int StageIndex { get; private set; }
        double timeRemaining = 7200;
        public float TimeRemaining { get { return (float)timeRemaining; } }
        public bool IsVictory { get; private set; }
        public bool IsGameOver { get { return IsVictory || HeroHealth <= 0 || TimeRemaining <= 0; } }
        public bool HeroWalking { get; private set; }
        public int AttackSerial { get; private set; }
        public int HeroDamageSerial { get; private set; }
        public float HeroHurtTimer { get; private set; }
        public int LastSlashDirection { get; private set; }
        public float StageStart { get { return StageIndex * StageLength; } }
        public float StageEnd { get { return StageStart + StageLength; } }
        public float MinY { get; private set; }
        public float MaxY { get; private set; }
        public bool StageCleared { get { return Enemies.TrueForAll(e => e.Health <= 0); } }
        RescueEnemy comboTarget;

        public RescueCombat(float minY = 0f, float maxY = 4f)
        {
            MinY = minY;
            MaxY = Mathf.Max(minY, maxY);
            HeroPosition = new Vector2(0, (MinY + MaxY) * .5f);
            SpawnStage();
        }

        public bool TryAttack()
        {
            if (IsGameOver) return false;
            LastSlashDirection = AttackSerial % 2 == 0 ? -1 : 1;
            AttackSerial++;
            bool hit = false;
            foreach (var enemy in Enemies)
            {
                var delta = enemy.Position - HeroPosition;
                if (!enemy.IsVulnerable || !Within(delta, HitRange) || delta.x * Facing < -.12f) continue;
                enemy.Health--;
                enemy.HealthVisible = true;
                enemy.ModeTime = DamageDuration;
                enemy.KnockbackDirection = Facing;
                enemy.Mode = enemy.Health <= 0 ? RescueEnemyMode.Dead : RescueEnemyMode.Damaged;
                // A successful hit interrupts the sword swing and its pending damage.
                enemy.AttackPending = false;
                enemy.AttackAge = 10f;
                enemy.AttackCooldown = 1f;
                comboTarget = enemy;
                hit = true;
            }
            return hit;
        }

        public void Tick(float dt, Vector2 movement)
        {
            HeroWalking = !IsGameOver && movement.sqrMagnitude > .001f;
            if (IsGameOver || dt <= 0 || float.IsNaN(dt) || float.IsInfinity(dt)) return;
            // Substeps keep enemy attacks, recovery and movement stable across frame rates.
            double budget = System.Math.Min(dt, timeRemaining);
            while (budget > 0 && !IsGameOver)
            {
                double preciseStep = System.Math.Min(budget, .02);
                float step = (float)preciseStep;
                budget -= preciseStep;
                timeRemaining = System.Math.Max(0, timeRemaining - preciseStep);
                HeroHurtTimer = Mathf.Max(0, HeroHurtTimer - step);
                if (TimeRemaining <= 0) break;
                MoveHero(step, movement);
                foreach (var enemy in Enemies) TickEnemy(enemy, step);
                if (StageCleared && HeroPosition.x > StageEnd)
                {
                    if (StageIndex == 2) IsVictory = true;
                    else { StageIndex++; SpawnStage(); }
                }
            }
        }

        void MoveHero(float dt, Vector2 movement)
        {
            movement = Vector2.ClampMagnitude(movement, 1);
            if (Mathf.Abs(movement.x) > .001f) Facing = movement.x > 0 ? 1 : -1;
            if (comboTarget != null && comboTarget.Mode == RescueEnemyMode.Damaged) movement.y = 0;
            var p = HeroPosition + movement * (HeroSpeed * dt);
            p.x = Mathf.Clamp(p.x, StageStart, StageEnd + (StageCleared ? .8f : 0));
            p.y = Mathf.Clamp(p.y, MinY, MaxY);
            HeroPosition = p;
        }

        void TickEnemy(RescueEnemy enemy, float dt)
        {
            if (enemy.Health <= 0) { enemy.Mode = RescueEnemyMode.Dead; return; }
            enemy.AttackAge = Mathf.Min(10, enemy.AttackAge + dt);
            if (enemy.Mode == RescueEnemyMode.Damaged)
            {
                // Knockback may leave the playable stage; normal movement never does.
                enemy.Position += Vector2.right * (enemy.KnockbackDirection * 1.35f * dt);
                enemy.ModeTime -= dt;
                if (enemy.ModeTime <= .0001f) { enemy.Mode = RescueEnemyMode.Lying; enemy.ModeTime = LyingDuration; }
                return;
            }
            if (enemy.Mode == RescueEnemyMode.Lying)
            {
                enemy.ModeTime -= dt;
                if (enemy.ModeTime <= .0001f) enemy.Mode = RescueEnemyMode.Returning;
                return;
            }
            // Recovery does not consume the enemy's attack cooldown.
            enemy.AttackCooldown = Mathf.Max(0, enemy.AttackCooldown - dt);
            Vector2 delta = HeroPosition - enemy.Position;
            if (enemy.AttackPending)
            {
                if (enemy.AttackAge < EnemyAttackWindup) return;
                enemy.AttackPending = false;
                if (Within(delta, HitRange))
                {
                    HeroHealth = Mathf.Max(0, HeroHealth - 20);
                    HeroDamageSerial++;
                    HeroHurtTimer = .3f;
                }
                return;
            }
            // Return from out-of-bounds knockback before attacking or chasing.
            if (enemy.Position.x < StageStart || enemy.Position.x > StageEnd)
            {
                enemy.Mode = RescueEnemyMode.Returning;
                MoveEnemy(enemy, new Vector2(Mathf.Clamp(enemy.Position.x, StageStart, StageEnd), enemy.Position.y), dt);
                return;
            }
            if (Within(delta, DetectionRange))
            {
                enemy.Mode = RescueEnemyMode.Chase;
                if (Mathf.Abs(delta.x) > .01f) enemy.Facing = delta.x > 0 ? 1 : -1;
                if (Within(delta, HitRange))
                {
                    if (enemy.AttackCooldown <= 0)
                    {
                        enemy.AttackPending = true;
                        enemy.AttackCooldown = 1f;
                        enemy.AttackSerial++;
                        enemy.AttackAge = 0;
                    }
                }
                else MoveEnemy(enemy, HeroPosition, dt);
            }
            else if (enemy.Mode == RescueEnemyMode.Returning || enemy.Mode == RescueEnemyMode.Chase)
            {
                enemy.Mode = RescueEnemyMode.Returning;
                MoveEnemy(enemy, enemy.PatrolOrigin, dt);
                if (Vector2.Distance(enemy.Position, enemy.PatrolOrigin) < .05f) enemy.Mode = RescueEnemyMode.Patrol;
            }
            else
            {
                enemy.Mode = RescueEnemyMode.Patrol;
                float left = Mathf.Max(StageStart, enemy.PatrolOrigin.x - 2.5f);
                float right = Mathf.Min(StageEnd, enemy.PatrolOrigin.x + 2.5f);
                if (enemy.Position.x <= left + .01f) enemy.Facing = 1;
                if (enemy.Position.x >= right - .01f) enemy.Facing = -1;
                var p = enemy.Position;
                p.x = Mathf.Clamp(p.x + enemy.Facing * .85f * dt, left, right);
                enemy.Position = p;
            }
        }

        void MoveEnemy(RescueEnemy enemy, Vector2 target, float dt)
        {
            if (Mathf.Abs(target.x - enemy.Position.x) > .01f) enemy.Facing = target.x > enemy.Position.x ? 1 : -1;
            var p = Vector2.MoveTowards(enemy.Position, target, 1.4f * dt);
            p.y = Mathf.Clamp(p.y, MinY, MaxY);
            enemy.Position = p;
        }

        void SpawnStage()
        {
            Enemies.Clear();
            comboTarget = null;
            for (int i = 0; i < 1; i++)
            {
                var origin = new Vector2(StageStart + 6 + i * 3, Mathf.Clamp((MinY + MaxY) * .5f + (i % 2 == 0 ? 0 : .8f), MinY, MaxY));
                Enemies.Add(new RescueEnemy { Position = origin, PatrolOrigin = origin });
            }
        }

        static bool Within(Vector2 delta, float range) { return Mathf.Abs(delta.x) <= range && Mathf.Abs(delta.y) <= range; }
    }
}
