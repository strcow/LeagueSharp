using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

namespace CowAntiMelee
{
    class Program
    {
        private static Obj_AI_Hero Player => ObjectManager.Player;
        public static List<Obj_AI_Hero> Enemies = new List<Obj_AI_Hero>();
        private static Orbwalking.Orbwalker _orbwalker;
        private static int DodgeRange => _menu.Item("antiMeleeRange").GetValue<Slider>().Value;
        private static bool _blockAttack;
        private static Menu _menu;
        private static int _dodgeTime;

        static void Main()
        {
            CustomEvents.Game.OnGameLoad += OnLoad;
        }

        private static void OnLoad(EventArgs args)
        {
            _menu = new Menu("Cow Anti Melee", "cowAntiMelee" + ObjectManager.Player.ChampionName, true);
            _menu.AddItem(new MenuItem("antiMeleeRange", "Avoid Range").SetValue(new Slider(420, 0,(int) Player.AttackRange)));
            _menu.AddItem(new MenuItem("positioningAssistant", "Enabled").SetValue(true));

            foreach (var hero in ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.IsEnemy))
            {
                Enemies.Add(hero);
                _menu.SubMenu("Positioning Assistant:").AddItem(new MenuItem("posAssistant" + hero.ChampionName, hero.ChampionName).SetValue(true));
            }

            _menu.AddToMainMenu();

            Game.OnUpdate += OnUpdate;
            Obj_AI_Base.OnIssueOrder += OnIssueOrder;
            Drawing.OnDraw += OnDraw;
        }

        private static void OnDraw(EventArgs args)
        {
            if (Utils.GameTimeTickCount - _dodgeTime <= 1000)
            {
                var wts = Drawing.WorldToScreen(Player.Position);
                Drawing.DrawText(wts.X, wts.Y, System.Drawing.Color.Red, "Anti Melee Activated");
            }
        }

        private static void OnIssueOrder(Obj_AI_Base sender, GameObjectIssueOrderEventArgs args)
        {
            if (!sender.IsMe)
                return;

            if (_blockAttack && args.IsAttackMove)
            {
                args.Process = false;
            }
        }

        private static List<Vector3> CirclePoints(float circleLineSegmentN, float radius, Vector3 position)
        {
            List<Vector3> points = new List<Vector3>();
            for (var i = 1; i <= circleLineSegmentN; i++)
            {
                var angle = i * 2 * Math.PI / circleLineSegmentN;
                var point = new Vector3(position.X + radius * (float)Math.Cos(angle), position.Y + radius * (float)Math.Sin(angle), position.Z);
                points.Add(point);
            }
            return points;
        }

        private static void OnUpdate(EventArgs args)
        {
            if (Player.ChampionName == "Draven" || Player.IsMelee || !_menu.Item("positioningAssistant").GetValue<bool>())
                return;

            _orbwalker = Orbwalking.Orbwalker.Instances.FirstOrDefault();

            if (_orbwalker == null)
                return;

            _orbwalker.SetOrbwalkingPoint(Vector3.Zero);

            foreach (var enemy in Enemies.Where(enemy => enemy.IsMelee && enemy.IsValidTarget(DodgeRange) && enemy.IsFacing(Player)  && _menu.Item("posAssistant" + enemy.ChampionName).GetValue<bool>()))
            {
                if (Player.FlatMagicDamageMod > Player.FlatPhysicalDamageMod)
                    _blockAttack = true;

                var points = CirclePoints(20, 200, Player.Position);
                Vector3 bestPoint = Vector3.Zero;

                foreach (var point in points)
                {
                    if (enemy.Distance(point) > DodgeRange && (bestPoint == Vector3.Zero || Game.CursorPos.Distance(point) < Game.CursorPos.Distance(bestPoint)))
                    {
                        bestPoint = point;
                    }
                }

                if (enemy.Distance(bestPoint) > DodgeRange)
                {
                    _orbwalker.SetOrbwalkingPoint(bestPoint);
                }
                else
                {
                    var fastPoint = enemy.ServerPosition.Extend(Player.ServerPosition, DodgeRange);
                    if (fastPoint.CountEnemiesInRange(DodgeRange) <= Player.CountEnemiesInRange(DodgeRange))
                        _orbwalker.SetOrbwalkingPoint(fastPoint);
                }
                _dodgeTime = Utils.GameTimeTickCount;
                return;
            }

            if (_blockAttack)
                _blockAttack = false;
        }
    }
}
