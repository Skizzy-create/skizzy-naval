using System.Collections.Generic;
using System.Linq;

namespace ACA;

public static class ACAExtensions
{
	public static IEnumerable<AvailablePart> ACAParts(this List<AvailablePart> parts)
	{
		return (from avPart in parts
			where avPart.partPrefab
			let Tiedown = avPart.partPrefab.GetComponent<Tiedown>()
			let FuelCap = avPart.partPrefab.GetComponent<FuelCap>()
			let FuelNozzle = avPart.partPrefab.GetComponent<FuelNozzle>()
			let CLLS = avPart.partPrefab.GetComponent<CLLS>()
			let FixPoint = avPart.partPrefab.GetComponent<FixPoint>()
			let ACE = avPart.partPrefab.GetComponent<ACE>()
			let Catapult = avPart.partPrefab.GetComponent<Catapult>()
			let Arrestwire = avPart.partPrefab.GetComponent<Arrestwire>()
			let ReloadableRail = avPart.partPrefab.GetComponent<ReloadableRail>()
			let Tailhook = avPart.partPrefab.GetComponent<Tailhook>()
			let OLS = avPart.partPrefab.GetComponent<OLS>()
			let otherModule = avPart.partPrefab.GetComponent<ACACategoryModule>()
			where (bool)Tiedown || (bool)FuelCap || (bool)FuelNozzle || (bool)CLLS || (bool)FixPoint || (bool)ACE || (bool)Catapult || (bool)Arrestwire || (bool)ReloadableRail || (bool)Tailhook || (bool)OLS || (bool)otherModule
			select avPart).AsEnumerable();
	}
}
