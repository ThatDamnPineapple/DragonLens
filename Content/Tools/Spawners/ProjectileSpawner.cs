﻿using DragonLens.Content.Filters;
using DragonLens.Content.Filters.ProjectileFilters;
using DragonLens.Content.GUI;
using DragonLens.Content.GUI.FieldEditors;
using DragonLens.Core.Loaders.UILoading;
using DragonLens.Core.Systems.ToolSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.UI.Elements;
using Terraria.UI;

namespace DragonLens.Content.Tools.Spawners
{
	internal class ProjectileSpawner : Tool
	{
		public override string IconKey => "ProjectileSpawner";

		public override string DisplayName => "Projectile spawner";

		public override string Description => "Spawn projectiles, with options for setting velocity and other parameters";

		public override void OnActivate()
		{
			ProjectileBrowser state = UILoader.GetUIState<ProjectileBrowser>();
			state.visible = !state.visible;

			BrowserButton.drawDelayTimer = 2;

			if (!state.initialized)
			{
				UILoader.GetUIState<ProjectileBrowser>().Refresh();
				state.initialized = true;
			}
		}
	}

	internal class ProjectileBrowser : Browser
	{
		public static Projectile selected;

		public static Vector2 velocity;
		public static Vector2Editor velocityEditor;

		public static float ai0;
		public static FloatEditor ai0Editor;

		public static float ai1;
		public static FloatEditor ai1Editor;

		public override string Name => "Projectile spawner";

		public override string IconTexture => "ProjectileSpawner";

		public override Vector2 DefaultPosition => new(0.5f, 0.4f);

		public override void PostInitialize()
		{
			velocityEditor = new("Velocity", n => velocity = n, Vector2.Zero);
			Append(velocityEditor);

			ai0Editor = new("ai 0", n => ai0 = n, 0);
			Append(ai0Editor);

			ai1Editor = new("ai 1", n => ai1 = n, 0);
			Append(ai1Editor);
		}

		public override void AdjustPositions(Vector2 newPos)
		{
			base.AdjustPositions(newPos);

			float nextY = 0;

			velocityEditor.Left.Set(newPos.X - 160, 0);
			velocityEditor.Top.Set(newPos.Y + nextY, 0);
			nextY += velocityEditor.Height.Pixels + 4;

			ai0Editor.Left.Set(newPos.X - 160, 0);
			ai0Editor.Top.Set(newPos.Y + nextY, 0);
			nextY += ai0Editor.Height.Pixels + 4;

			ai1Editor.Left.Set(newPos.X - 160, 0);
			ai1Editor.Top.Set(newPos.Y + nextY, 0);
		}

		public override void PopulateGrid(UIGrid grid)
		{
			var buttons = new List<ProjectileButton>();
			// `0` corresponds to ProjectileID.None - that is, no projectile.
			for (int k = 1; k < ProjectileLoader.ProjectileCount; k++)
			{
				var proj = new Projectile();
				proj.SetDefaults(k);

				buttons.Add(new ProjectileButton(proj, this));
			}

			grid.AddRange(buttons);
		}

		public override void SetupFilters(FilterPanel filters)
		{
			filters.AddSeperator("Mod filters");
			filters.AddFilter(new Filter("DragonLens/Assets/Filters/Vanilla", "Vanilla", "Projectiles from the base game", n => !(n is ProjectileButton && (n as ProjectileButton).proj.ModProjectile is null)));

			foreach (Mod mod in ModLoader.Mods.Where(n => n.GetContent<ModProjectile>().Count() > 0))
			{
				filters.AddFilter(new ProjectileModFilter(mod));
			}

			filters.AddSeperator("Friendly/Hostile filters");
			filters.AddFilter(new Filter("DragonLens/Assets/Filters/Friendly", "Friendly", "Projectiles which by default belong to a player", n => !(n is ProjectileButton && (n as ProjectileButton).proj.friendly)));
			filters.AddFilter(new Filter("DragonLens/Assets/Filters/Hostile", "Hostile", "Projectiles which by default belong to an enemy", n => !(n is ProjectileButton && (n as ProjectileButton).proj.hostile)));
		}

		public override void SafeUpdate(GameTime gameTime)
		{
			base.SafeUpdate(gameTime);

			if (selected != null)
				Main.LocalPlayer.mouseInterface = true;
		}

		public override void Click(UIMouseEvent evt)
		{
			base.Click(evt);

			if (selected != null)
				Projectile.NewProjectile(null, Main.MouseWorld, velocity, selected.type, selected.damage, selected.knockBack, Main.myPlayer, ai0, ai1);
		}

		public override void RightClick(UIMouseEvent evt)
		{
			if (selected != null)
				selected = null;
		}

		public override void Draw(SpriteBatch spriteBatch)
		{
			if (selected != null)
			{
				Main.instance.LoadProjectile(selected.type);
				Texture2D tex = Terraria.GameContent.TextureAssets.Projectile[selected.type].Value;

				float scale = 1;
				if (tex.Width > 32 || tex.Height > 32)
					scale = 32f / Math.Max(tex.Width, tex.Height);

				spriteBatch.Draw(tex, Main.MouseScreen + Vector2.One * 8 + tex.Size(), new Rectangle(0, 0, tex.Width, tex.Height), Color.White * 0.5f, 0, new Vector2(tex.Width, tex.Height) / 2, scale, 0, 0);
			}

			base.Draw(spriteBatch);
		}
	}

	internal class ProjectileButton : BrowserButton
	{
		public Projectile proj;
		public string name;

		public override string Identifier => name;

		public ProjectileButton(Projectile proj, Browser browser) : base(browser)
		{
			this.proj = proj;

			try
			{
				name = proj.Name;
			}
			catch
			{
				Main.NewText($"A Projectiles ({proj.ModProjectile.Name}) name threw an exception while getting it! Report to {proj.ModProjectile.Mod.DisplayName} developers!");
				name = $"This Projectiles name threw an exception while getting it! Report to {proj.ModProjectile.Mod.DisplayName} developers!";
			}
		}

		public override void SafeDraw(SpriteBatch spriteBatch, Rectangle iconBox)
		{
			Main.instance.LoadProjectile(proj.type);
			Texture2D tex = Terraria.GameContent.TextureAssets.Projectile[proj.type].Value;

			float scale = 1;
			if (tex.Width > 32 || tex.Height > 32)
				scale = 32f / Math.Max(tex.Width, tex.Height);

			spriteBatch.Draw(tex, iconBox.Center(), new Rectangle(0, 0, tex.Width, tex.Height), Color.White, 0, new Vector2(tex.Width, tex.Height) / 2, scale, 0, 0);

			if (IsMouseHovering)
			{
				Tooltip.SetName(proj.Name);
				Tooltip.SetTooltip($"Type: {proj.type}");
			}
		}

		public override void Click(UIMouseEvent evt)
		{
			ProjectileBrowser.selected = proj;
			Main.NewText($"{proj.Name} selected, click anywhere in the world to spawn. Right click to deselect.");
		}

		public override void RightClick(UIMouseEvent evt)
		{

		}

		public override int CompareTo(object obj)
		{
			return proj.type - (obj as ProjectileButton).proj.type;
		}
	}
}
