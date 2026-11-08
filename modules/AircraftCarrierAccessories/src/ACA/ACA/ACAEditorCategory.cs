using System.Collections.Generic;
using KSP.UI.Screens;
using RUI.Icons.Selectable;
using UnityEngine;

namespace ACA;

[KSPAddon(KSPAddon.Startup.MainMenu, true)]
public class ACAEditorCategory : MonoBehaviour
{
	private static readonly List<AvailablePart> availableParts = new List<AvailablePart>();

	private void Awake()
	{
		GameEvents.onGUIEditorToolbarReady.Add(ACAWeaponsCategory);
	}

	private void ACAWeaponsCategory()
	{
		availableParts.Clear();
		availableParts.AddRange(PartLoader.LoadedPartsList.ACAParts());
		Debug.Log(availableParts.Count);
		Texture2D texture = GameDatabase.Instance.GetTexture("KFC/Textures/icon", asNormalMap: false);
		Icon icon = new Icon("ACA", texture, texture);
		PartCategorizer.Category mainFilter = PartCategorizer.Instance.filters.Find((PartCategorizer.Category f) => f.button.categorydisplayName == "#autoLOC_453547");
		PartCategorizer.AddCustomSubcategoryFilter(mainFilter, "ACA", "Aircraft Carrier Accessories", icon, (AvailablePart p) => availableParts.Contains(p));
	}
}
