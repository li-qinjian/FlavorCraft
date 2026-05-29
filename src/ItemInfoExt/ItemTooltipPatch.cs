using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem.ViewModelCollection.Inventory;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Localization;

namespace ItemTooltipInfo
{
	[HarmonyPatch]
	public static class ItemTooltipPatch
	{
		public static IEnumerable<MethodBase> TargetMethods()
		{
			const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

			foreach (MethodInfo method in typeof(ItemMenuVM).GetMethods(flags))
			{
				if (!method.Name.StartsWith("Set", StringComparison.Ordinal) || !method.Name.EndsWith("Tooltip", StringComparison.Ordinal))
				{
					continue;
				}

				if (method.Name == "SetWeaponComponentTooltip")
				{
					continue;
				}

				yield return method;
			}
		}

		private static void ResolveOnce()
		{
			if (_resolved)
			{
				return;
			}

			_addStringProperty = typeof(ItemMenuVM).GetMethod("AddComparableStringProperty", BindingFlags.Instance | BindingFlags.NonPublic);
			_targetItemField = typeof(ItemMenuVM).GetField("_targetItem", BindingFlags.Instance | BindingFlags.NonPublic);
			//_textID = new TextObject("{=ItemIDTooltip_ID}Item ID:", null);
			_textCulture = new TextObject("{=ItemIDTooltip_Culture}Culture:", null);
			//_textCategory = new TextObject("{=ItemIDTooltip_Category}Category:", null);
			_resolved = true;
		}

		[HarmonyPostfix]
		private static void Postfix(ItemMenuVM __instance)
		{
			try
			{
				ResolveOnce();

				MethodInfo? addStringProperty = _addStringProperty;
				FieldInfo? targetItemField = _targetItemField;
				//TextObject? textID = _textID;
				TextObject? textCulture = _textCulture;
				//TextObject? textCategory = _textCategory;

				if (addStringProperty == null || targetItemField == null || /*textID == null ||*/ textCulture == null /*|| textCategory == null*/)
				{
					return;
				}

				if (targetItemField.GetValue(__instance) is not ItemVM itemVM)
				{
					return;
				}

				//AddProperty(__instance, addStringProperty, textID, GetID);
				AddProperty(__instance, addStringProperty, textCulture, GetCulture);
				//AddProperty(__instance, addStringProperty, textCategory, GetCategory);
			}
			catch (Exception)
			{
				// Keep tooltip patch failures from breaking the inventory UI.
			}
		}

		private static void AddProperty(ItemMenuVM instance, MethodInfo addStringProperty, TextObject label, Func<EquipmentElement, string> valueGetter)
		{
			addStringProperty.Invoke(instance, new object[]
			{
				label,
				valueGetter,
				DummyValue
			});
		}

		private static MethodInfo? _addStringProperty;

		private static FieldInfo? _targetItemField;

		private static bool _resolved = false;

		//private static TextObject? _textID;

		private static TextObject? _textCulture;

		//private static TextObject? _textCategory;

		private static readonly Func<EquipmentElement, int> DummyValue = _ => 0;

		//private static readonly Func<EquipmentElement, string> GetID = e => e.Item?.StringId ?? "N/A";

		private static readonly Func<EquipmentElement, string> GetCulture = e => e.Item?.Culture?.Name?.ToString() ?? "Cultureless";

		//private static readonly Func<EquipmentElement, string> GetCategory = e => e.Item?.ItemCategory?.GetName()?.ToString() ?? "N/A";
	}
}
