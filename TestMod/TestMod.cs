using FRML;
using System;

public class TestMod {
	public static void Init() {
		// Texture override example
		// This replaces Garfield's specular texture with a white image, making him very shiny
		// Please note white.png must exist in the game's root directory
		ModLoader.Texture("C_Garfield_S", "white.png");

		// Register callbacks
		ModLoader.Register("KartBonusMgr", "DoSetItem", OnDoSetItem);
		ModLoader.Register("KartBonusMgr", "DoActivateBonus", OnDoActivateBonus);
	}

	public static int OnDoSetItem(object _self, object[] param) {
		ModLoader.Log(String.Format("Driver with ID={0} just picked up a {1}.\n", (int)param[0], ((BonusCategory)(byte)param[1]).ToString()));
		return 0;
	}

	public static int OnDoActivateBonus(object self, object[] param) {
		// heres how you can get a private member
		int id = ((KartBonusMgr)self).GetMember<Kart>("m_kart").DriverId;
		ModLoader.Log(String.Format("Driver with ID={0} just activated an item.\n", id));
		return 0;
	}
}
