using System;
using System.Collections.Generic;
using System.Linq;

using Color = System.Drawing.Color;
using System;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Menu.Values;
using SharpDX;
using EloBuddy.SDK.Rendering;
namespace SonaEB
{
	class Program
	{
		const string version = "1.0.0.2";
		static Spell.Active Q, W, E;
		static Spell.Skillshot R;
		static Menu Menu, SettingsMenu, DrawMenu, MiscMenu, SkinMenu;
		static Slider skinSelect, skinSelect2, R_enemies_count;
		static Vector3 mousePos = Game.CursorPos;
		static AIHeroClient Player = ObjectManager.Player;
		static CheckBox R_Combo, R_Smart_Combo;
		static float Dmg = 0f;
		static string[] skins = {"Classic", "Muse", "Pentakill", "Silent Night", "Guqin", "Arcade", "DJ"};
		static string[] DJTrans = {"Kinetic", "Concussive", "Ethereal"};
		static void Main(string[] args)
		{
			Loading.OnLoadingComplete += Loading_OnLoadingComplete;
			// Bootstrap.Init(null);
		}

		private static void Loading_OnLoadingComplete(EventArgs args)
		{
			if (Player.ChampionName != "Sona") return;

			Q = new Spell.Active(SpellSlot.Q, 850);
			W = new Spell.Active(SpellSlot.W, 1000);
			E = new Spell.Active(SpellSlot.E, 350);
			R = new Spell.Skillshot(SpellSlot.R, 1000, SkillShotType.Linear, 200, 2400, 140);

			Menu = MainMenu.AddMenu("EB Sona", "SonaEB");
			Menu.AddGroupLabel("EB Sona "+version);
			Menu.AddSeparator();
			Menu.AddLabel("By Onin");
			SettingsMenu = Menu.AddSubMenu("Settings", "Settings");
			SettingsMenu.AddGroupLabel("Settings");
			SettingsMenu.AddLabel("Combo");
			SettingsMenu.Add("Q_Combo", new CheckBox("Use Q on Combo"));
			// SettingsMenu.Add("E_Combo", new CheckBox("Use E on Combo"));
			R_Smart_Combo = SettingsMenu.Add("R_Smart_Combo", new CheckBox("Use Smart R on Combo",false));
			R_Smart_Combo.OnValueChange += delegate
			{
				if (R_Smart_Combo.CurrentValue)
					R_Combo.CurrentValue = false;
				else
					if (R_Combo.CurrentValue == false)
						R_Combo.CurrentValue = true;
			};
			R_Combo = SettingsMenu.Add("R_Combo", new CheckBox("Use R on Combo"));
			R_Combo.OnValueChange += delegate
			{
				if (R_Combo.CurrentValue)
				{
					R_Smart_Combo.CurrentValue = false;
					R_enemies_count.IsVisible = true;
				}
				else
				{
					R_enemies_count.IsVisible = false;
					if (R_Smart_Combo.CurrentValue == false)
						R_Smart_Combo.CurrentValue = true;
				}
			};
			R_enemies_count = SettingsMenu.Add("ChangeSkin", new Slider("R Combo Count 1", 1, 1, 5));
			R_enemies_count.OnValueChange += delegate(ValueBase<int> sender, ValueBase<int>.ValueChangeArgs a)
			{
				R_enemies_count.DisplayName = "R Combo Count "+ a.NewValue;
			};
			
			SettingsMenu.AddSeparator();
			SettingsMenu.AddLabel("KillSteal");
			SettingsMenu.Add("Q_KillSteal", new CheckBox("Use Q KillSteal",false));
			
			SettingsMenu.AddSeparator();
			SettingsMenu.AddLabel("Harass");
			SettingsMenu.Add("Q_Harass", new CheckBox("Use Q on Harass"));
			
			MiscMenu = Menu.AddSubMenu("Misc");
			MiscMenu.Add("AntiGapCloser", new CheckBox("Anti GapCloser"));
			MiscMenu.Add("SmartHeal", new CheckBox("Smart Heal"));
			MiscMenu.Add("SaveAllyR", new CheckBox("Save dying ally R"));
			MiscMenu.Add("InterruptR", new CheckBox("Interrupt R"));
			
			DrawMenu = Menu.AddSubMenu("Drawing settings", "DrawingSettings");
			DrawMenu.AddGroupLabel("Drawins settings");
			DrawMenu.AddSeparator();
			DrawMenu.Add("Available_Draw", new CheckBox("Only Draw Available Skill",false));
			DrawMenu.Add("Q_Draw", new CheckBox("Draw Q range",false));
			DrawMenu.Add("W_Draw", new CheckBox("Draw W & R range",false));
			DrawMenu.Add("E_Draw", new CheckBox("Draw E range",false));
			
			SkinMenu = Menu.AddSubMenu("Skin Change");
			SkinMenu.AddGroupLabel("Skins");
			
			skinSelect = SkinMenu.Add("ChangeSkin", new Slider("DJ Sona", 6, 0, 6));
			skinSelect.OnValueChange += delegate(ValueBase<int> sender, ValueBase<int>.ValueChangeArgs a)
			{
				skinSelect.DisplayName = skins[a.NewValue]+ " Sona";
				
				if (a.NewValue == 6)
				{
					skinSelect2.IsVisible= true;
					DJTransform(skinSelect2.CurrentValue);
					Player.SetSkinId(a.NewValue);
				}
				else
				{
					skinSelect2.IsVisible= false;
					Player.SetSkin("Sona", a.NewValue);
				}
				
			};
			//Player.SetModel("SonaDJGenre01");
			//Player.SetSkinId(6);
			Player.SetSkin("SonaDJGenre01", 6);
			
			SkinMenu.AddSeparator();
			SkinMenu.AddGroupLabel("DJ Sona Transformation");
			
			skinSelect2 = SkinMenu.Add("DJSonaChangeSkin", new Slider("Kinetic", 0, 0, 2));
			skinSelect2.OnValueChange += delegate(ValueBase<int> sender, ValueBase<int>.ValueChangeArgs a)
			{
				skinSelect2.DisplayName = DJTrans[a.NewValue];
				if (Player.SkinId == 6)
					DJTransform(a.NewValue);
			};
			
			Game.OnTick += Game_OnTick;
			Drawing.OnDraw += OnDraw;
			Gapcloser.OnGapcloser += GapCloser;
			Obj_AI_Base.OnProcessSpellCast += OnProcessSpellCast;
			Interrupter.OnInterruptableSpell += Interrupter_OnInterruptableSpell;
			Chat.Print( "<font color='#FFFFFF'> EB Sona "+ version + "</font>");
		}

		static void DJTransform(int index)
		{
			switch (index)
			{
					case 0:Player.SetModel("SonaDJGenre01");break;
					case 1:Player.SetModel("SonaDJGenre02");break;
					case 2:Player.SetModel("SonaDJGenre03");break;
			}
		}
		public static int CountEnemiesInRange(Vector3 CastStartPosition , Vector3 CastEndPosition, float width)
		{
			var target = EntityManager.Heroes.Enemies
				.FindAll(a =>a.IsValidTarget() && a.Position.To2D().Distance(CastStartPosition.To2D(), CastEndPosition.To2D(), true) < width);
			if (target != null)
				return target.Count();
			return 0;
			
		}
		static void R_Logic()
		{
			if (SettingsMenu["R_Smart_Combo"].Cast<CheckBox>().CurrentValue && R.IsReady())
			{
				var target2 = EntityManager.Heroes.Enemies.Where(a => a.IsValidTarget() && a.Distance(Player) < R.Range+600);
				
				var target = EntityManager.Heroes.Enemies
					.Where(a => a.IsValidTarget() && a.Distance(Player) < R.Range)
					.OrderByDescending( a => a.Distance(Player));
				
				if (target == null) return;
				
				if (CountEnemiesInRange(Player.Position, target.FirstOrDefault().Position, R.Width) == target.Count()
				    && target2.Count() == target.Count())
				{
					var pred = R.GetPrediction(target.FirstOrDefault());
					if (pred.HitChance >= HitChance.High)
						R.Cast(pred.CastPosition);
				}
			}
		}
		public static void Interrupter_OnInterruptableSpell(Obj_AI_Base sender, Interrupter.InterruptableSpellEventArgs e)
		{
			
			if (MiscMenu["InterruptR"].Cast<CheckBox>().CurrentValue && R.IsReady() && e.DangerLevel.HasFlag(DangerLevel.High) &&
			    sender.IsValidTarget(R.Range) && sender.Team != Player.Team)
			{
				var ally = EntityManager.Heroes.Allies.FirstOrDefault(x => x.Distance(sender) < 600);
				if (ally == null) return;
				
				var pred = R.GetPrediction(sender);
				if (pred.HitChance >= HitChance.High)
					R.Cast(pred.CastPosition);
			}
		}
		public static void OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
		{
			//Chat.Print(sender.BaseSkinName);
			if (sender.IsEnemy  && !Player.IsDead  && !args.Target.IsDead && 
			    args.Target.Type == GameObjectType.AIHeroClient && sender.Type == GameObjectType.AIHeroClient &&
			    ((args.Target.IsMe) || (args.Target.IsAlly && Player.Distance(args.Target) < W.Range-50))    )
			{
				
				//if ((args.Target.IsMe) || (args.Target.IsAlly && Player.Distance(args.Target) < W.Range-50)) return;
				
				var attackerHero = EntityManager.Heroes.AllHeroes.First(hero => hero.NetworkId == sender.NetworkId);
				var attackedHero = EntityManager.Heroes.AllHeroes.First(hero => hero.NetworkId == args.Target.NetworkId);
				
				if ( MiscMenu["SaveAllyR"].Cast<CheckBox>().CurrentValue && R.IsReady() && attackerHero.Distance(Player) < R.Range-50)
				{
					SpellDataInst spellA = sender.Spellbook.Spells.FirstOrDefault(hero=> args.SData.Name.Contains(hero.SData.Name));
					
					SpellSlot spellSlot = spellA == null ? SpellSlot.Unknown : spellA.Slot;
					SpellSlot igniteSlot = attackerHero.GetSpellSlotFromName("SummonerDot");
					Dmg = 0f;
					
					
					if (igniteSlot != SpellSlot.Unknown && spellSlot == igniteSlot)
					{
						Dmg = attackerHero.GetSummonerSpellDamage( attackedHero, DamageLibrary.SummonerSpells.Ignite);
					}

					else if (spellSlot == SpellSlot.Item1 || spellSlot == SpellSlot.Item2 || spellSlot == SpellSlot.Item3 || spellSlot == SpellSlot.Item4 || spellSlot == SpellSlot.Item5 || spellSlot == SpellSlot.Item6)
					{
						if (args.SData.Name == "king")
						{
							Dmg = attackerHero.GetItemDamage( attackedHero, ItemId.Blade_of_the_Ruined_King);
						}
						else if (args.SData.Name == "bilge")
						{
							Dmg = attackerHero.GetItemDamage( attackedHero, ItemId.Bilgewater_Cutlass);
						}
						else if (args.SData.Name == "hydra")
						{
							Dmg = attackerHero.GetItemDamage( attackedHero, ItemId.Ravenous_Hydra_Melee_Only);
						}
						else
							Chat.Print("Items :"+ args.SData.Name);
					}
					else if (spellSlot == SpellSlot.Unknown)
					{
						Dmg = attackerHero.GetAutoAttackDamage(attackedHero, true);
					}
					else
					{
						Dmg = attackerHero.GetSpellDamage(attackedHero, spellSlot, DamageLibrary.SpellStages.Default);
					}
					
					if (Dmg > attackedHero.Health-20)
					{
						var pred = R.GetPrediction(attackerHero);
						if (pred.HitChance >= HitChance.High)
							R.Cast(pred.CastPosition);
					}
				}
				
				
				if (MiscMenu["SmartHeal"].Cast<CheckBox>().CurrentValue && W.IsReady())
				{
					if (attackedHero.HealthPercent < 70 && attackedHero.Distance(Player) < 350 && Player.ManaPercent > 40)
					{
						W.Cast();
					}
					else if (attackedHero.HealthPercent < 90 && attackedHero.Distance(Player) < 350 && Player.ManaPercent > 85)
					{
						W.Cast();
					}
				}
			}
			
		}
		private static void Game_OnTick(EventArgs args)
		{
			
			KillSteal();
			
			
			if (MiscMenu["SmartHeal"].Cast<CheckBox>().CurrentValue && W.IsReady())
			{
				var ally = EntityManager.Heroes.Allies
					.FirstOrDefault(x => x.HealthPercent < 20 && x.Distance(Player) < W.Range-50 && !x.IsDead);
				if (ally != null)
					W.Cast();
			}
			if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
			{
				Combo();
				return;
			}
			if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass))
			{
				Harass();
			}
		}
		
		private static void Combo()
		{
			var target = TargetSelector.GetTarget(R.Range, DamageType.Magical);
			var useQ = SettingsMenu["Q_Combo"].Cast<CheckBox>().CurrentValue;
			var useR = SettingsMenu["R_Combo"].Cast<CheckBox>().CurrentValue;
			if (useQ && Q.IsReady() && !target.IsDead && !target.IsZombie && target.IsValidTarget(Q.Range))
			{
				Q.Cast();
				Core.DelayAction(() => EloBuddy.Player.IssueOrder(GameObjectOrder.AttackUnit, target),300);
			}
			
			if (useR && !target.IsDead && !target.IsZombie && target.IsValidTarget(R.Range))
			{
				var Rtarget = EntityManager.Heroes.Enemies.Where(a => a.IsValidTarget(R.Range)
				                                                 && !a.IsDead && !a.IsZombie)
					.OrderByDescending( a => a.Distance(Player));
				
				if (Rtarget == null && Rtarget.Count()< R_enemies_count.CurrentValue) return;
				
				var pred = R.GetPrediction(Rtarget.FirstOrDefault());
				if (pred.HitChance < HitChance.Collision) return;
				var R_furthest_target = pred.CastPosition;
				int enemy_in_R = 0;
				foreach (var enemy in Rtarget)
				{
					var pred2 = R.GetPrediction(enemy);
					if (pred2.HitChance >= HitChance.Collision)
						if (pred2.CastPosition.Distance(Player.Position.To2D(), R_furthest_target.To2D() ) < R.Width)
							enemy_in_R += 1;
				}
				
				if (enemy_in_R >= R_enemies_count.CurrentValue && R.IsReady() )
					R.Cast(R_furthest_target);
			}
			else
				R_Logic();
			/*
            foreach (var element in target.Buffs) {
            		Chat.Print(element.Name);}
			 */
		}
		private static void Harass()
		{
		
			if (SettingsMenu["Q_Harass"].Cast<CheckBox>().CurrentValue && Q.IsReady())
			{
				var target = TargetSelector.GetTarget(Q.Range, DamageType.Magical);
				if (target.IsValidTarget())
					Q.Cast();
			}
		}
		private static void LaneClear()
		{
			if (SettingsMenu["Q_LaneClear"].Cast<CheckBox>().CurrentValue && Q.IsReady())
			{
				var	minions = ObjectManager.Get<Obj_AI_Minion>()
					.Where(x => x.Distance(Player) < Q.Range && x.Team != Player.Team && !x.IsZombie && ! x.IsDead)
					.OrderBy(x => x.Distance(Player))
					.Take(2)
					.Where(x => Player.GetSpellDamage(x,SpellSlot.Q,DamageLibrary.SpellStages.Default) > x.Health) ;
				if (minions == null || !minions.Any()) return;
				
				if (minions.Count() > 1)
					Q.Cast();
				
			}
		}
		private static void OnDraw(EventArgs args)
		{
			if (DrawMenu["Q_Draw"].Cast<CheckBox>().CurrentValue && 
			    (DrawMenu["Available_Draw"].Cast<CheckBox>().CurrentValue ? Q.IsReady() : true))
			{
				new Circle() { Color = Color.GreenYellow, BorderWidth = 1, Radius = Q.Range }.Draw(Player.Position);
			}
			if (DrawMenu["W_Draw"].Cast<CheckBox>().CurrentValue && 
			    (DrawMenu["Available_Draw"].Cast<CheckBox>().CurrentValue ? W.IsReady() || R.IsReady() : true))
			{
				new Circle() { Color = Color.GreenYellow, BorderWidth = 1, Radius = W.Range }.Draw(Player.Position);
			}

			if (DrawMenu["E_Draw"].Cast<CheckBox>().CurrentValue && 
			    (DrawMenu["Available_Draw"].Cast<CheckBox>().CurrentValue ? E.IsReady() : true))
			{
				new Circle() { Color = Color.GreenYellow, BorderWidth = 1, Radius = E.Range }.Draw(Player.Position);
			}
			
			/*
			var	minions = ObjectManager.Get<Obj_AI_Minion>()
				.Where(x => x.Distance(Player) < Q.Range && x.Team != Player.Team && !x.IsZombie && ! x.IsDead)
				.OrderBy(x => x.Distance(Player))
				.Take(2)
				.Where(x => Player.GetSpellDamage(x,SpellSlot.Q,DamageLibrary.SpellStages.Default) > x.Health)
				.OrderByDescending(x => x.Health);
			if (minions == null) return;
			foreach (var element in minions) {
				new Circle() { Color = Color.GreenYellow, BorderWidth = 1, Radius = 100 }.Draw(element.Position);
			}
			*/
			
		}
		private static void GapCloser(AIHeroClient sender, Gapcloser.GapcloserEventArgs e)
		{
			if (MiscMenu["AntiGapCloser"].Cast<CheckBox>().CurrentValue && E.IsReady()
			    && sender.IsEnemy && Player.Distance(e.End) < 750 )
			{
				E.Cast();
			}
		}
		private static void KillSteal()
		{
			if (SettingsMenu["Q_KillSteal"].Cast<CheckBox>().CurrentValue && Q.IsReady())
			{
				var target = TargetSelector.GetTarget(Q.Range, DamageType.Magical);
				if ( Player.GetSpellDamage(target,SpellSlot.Q,DamageLibrary.SpellStages.Default) > target.Health)
				{
					Q.Cast();
				}
			}
		}
	}
}
