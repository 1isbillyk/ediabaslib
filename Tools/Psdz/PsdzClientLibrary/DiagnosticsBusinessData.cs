﻿using System;
using System.Collections.Generic;
using System.Globalization;
using BMW.Rheingold.CoreFramework.Contracts.Vehicle;
using PsdzClient.Core;

namespace PsdzClientLibrary
{
	public class DiagnosticsBusinessData
	{
		public static void SetSp2021Enabled(Vehicle vecInfo)
		{
			vecInfo.Sp2021Enabled = vecInfo.Produktlinie.StartsWith("21");
		}

		public static readonly List<string> Vin17WiesmannSgbdsToCheck = new List<string>
		{
			"D_0044",
			"EWS3"
		};

		public static readonly DateTime DTimeF01Lci = DateTime.ParseExact("01.07.2013", "dd.MM.yyyy", new CultureInfo("de-DE"));

		public static readonly DateTime DTimeRR_S2 = DateTime.ParseExact("01.06.2012", "dd.MM.yyyy", new CultureInfo("de-DE"));

		public static readonly DateTime DTimeF25Lci = DateTime.ParseExact("01.04.2014", "dd.MM.yyyy", new CultureInfo("de-DE"));

		public static readonly DateTime DTimeF01BN2020MostDomain = DateTime.ParseExact("30.06.2010", "dd.MM.yyyy", new CultureInfo("de-DE"));
	}
}
