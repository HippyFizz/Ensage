﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Ensage;
using SharpDX;
using SharpDX.Direct3D9;

namespace TinkerMadness
{
    class Program
    {
        const int WM_KEYUP = 0x0101;
        const int WM_KEYDOWN = 0x0105;

        private static Hero _target;
        private static bool activated;
        static void Main(string[] args)
        {
            Game.OnGameUpdate += ComboChecker;
        }

        /// <summary>
        /// Check if have the correct hero and our minimum combo available
        /// </summary>
        /// <param name="args"></param>
        static void ComboChecker(EventArgs args)
        {
            if (Game.IsInGame || Game.IsPaused)
                return;

            var me = EntityList.Hero;
            if (me == null)
                return;

            // wrong hero picked, disabled our script
            // enable on next pick again
            if (me.ClassId == 123)
            {
                Game.OnGameUpdate -= ComboChecker;
            }
            if (HasCombo())
            {
                Game.OnGameUpdate -= ComboChecker;
                Game.OnGameWndProc += Game_OnGameWndProc;
            }
        }

        static void Game_OnGameWndProc(WndProcEventArgs args)
        {
            if (args.MsgId != WM_KEYUP || args.WParam != 'O' || Game.IsChatOpen)
                return;

            // disable
            if (_target != null)
            {
                _target = null;
                return;
            }

            _target = GetClosestEnemyHeroToMouse();
            if (_target != null)
            {
                Game.OnGameUpdate += ComboTick;
            }
        }

        static void ComboTick(EventArgs args)
        {
            if (!Game.IsInGame)
            {
                Game.OnGameUpdate -= ComboTick;
                return;
            }
            if (Game.IsPaused)
                return;

            var me = EntityList.Hero;
            if (_target == null || !_target.IsValid || !_target.IsAlive || !me.IsAlive || !_target.IsVisible || _target.UnitState.HasFlag(UnitState.MagicImmune))
            {
                _target = null;
                Game.OnGameUpdate -= ComboTick;
                return;
            }

            var abilities = me.Spellbook.Spells.ToList();
            var Q = abilities[0];
            var W = abilities[1];
            var R = abilities[4];
            //if( R.IsInAbilityPhase || R.C)
        }

        static bool HasCombo()
        {
            var me = EntityList.GetLocalPlayer().Hero;
            if (me.Spellbook.Spells.Last().Level == 0)
                return false;

            // item_blink, item_sheepstick
            var items = me.Inventory.Items.ToList();
            return items.Any(x => x.Name == "item_blink") && items.Any(x => x.Name == "item_sheepstick");
        }

        static Item GetItem(string name)
        {
            return EntityList.GetLocalPlayer().Hero.Inventory.Items.ToList().FirstOrDefault(x => x.Name == name);
        }

        static Item GetDagon()
        {
            return EntityList.GetLocalPlayer().Hero.Inventory.Items.ToList().FirstOrDefault(x => x.Name.Substring(0,10) == "item_dagon");
        }

        static Hero GetClosestEnemyHeroToMouse()
        {
            var mousePosition = Game.MousePosition;
            var enemies = EntityList.GetEntities<Hero>().Where(x => x.IsVisible && x.IsAlive && !x.IsIllusion && x.Team != EntityList.Player.Team && !x.UnitState.HasFlag(UnitState.MagicImmune)).ToList();
            //enemies.Sort((h1, h2) => Vector3.DistanceSquared(mousePosition, h1.Position).CompareTo
            //    ((Vector3.DistanceSquared(mousePosition, h2.Position))));

            var minimumDistance = float.MaxValue;
            Hero result = null;
            foreach (var hero in enemies)
            {
                var distance = Vector3.DistanceSquared(mousePosition, hero.Position);
                if (result == null || distance < minimumDistance)
                {
                    minimumDistance = distance;
                    result = hero;
                }
            }
            return result;
        }

        float FindAngleR(Entity ent)
        {
            return (float)(ent.RotationRad < 0 ? Math.Abs(ent.RotationRad) : 2*Math.PI - ent.RotationRad);
        }

        float FindAngleBetween(Vector3 first, Vector3 second)
        {
            var xAngle = (float)(Math.Atan(Math.Abs(second.X - first.X) / Math.Abs(second.Y - first.Y)) * (180.0 / Math.PI));
            if (first.X <= second.X && first.Y >= second.Y)
                return 90 - xAngle;
            if (first.X >= second.X && first.Y >= second.Y)
                return xAngle + 90;
            if (first.X >= second.X && first.Y <= second.Y)
                return 90 - xAngle + 180;
            if (first.X <= second.X && first.Y <= second.Y)
                return xAngle + 90 + 180;
            return 0;
        }
    }
}
