namespace TeReoLocalizer.Annotations;

/// <summary>
/// Helper class for language management.
/// </summary>
public static class LanguagesCls
{
    /// <summary>
    /// Dictionary of ISO 3166-1-alpha-2 codes
    /// </summary>
    public static readonly Dictionary<Languages, string> LanguageCodesIso3166Alpha2 = new Dictionary<Languages, string>
    {
        { Languages.Unknown, "" },
        { Languages.AA, "DJ" }, // Afar - Djibouti
        { Languages.AB, "GE" }, // Abkhazian - Georgia
        { Languages.AF, "ZA" }, // Afrikaans - South Africa
        { Languages.AM, "ET" }, // Amharic - Ethiopia
        { Languages.AR, "SA" }, // Arabic - Saudi Arabia
        { Languages.AS, "IN" }, // Assamese - India
        { Languages.AY, "BO" }, // Aymara - Bolivia
        { Languages.AZ, "AZ" }, // Azerbaijani - Azerbaijan
        { Languages.BA, "RU" }, // Bashkir - Russia
        { Languages.BE, "BY" }, // Belarusian - Belarus
        { Languages.BG, "BG" }, // Bulgarian - Bulgaria
        { Languages.BH, "IN" }, // Bihari - India
        { Languages.BI, "VU" }, // Bislama - Vanuatu
        { Languages.BN, "BD" }, // Bengali - Bangladesh
        { Languages.BO, "CN" }, // Tibetan - China
        { Languages.BR, "FR" }, // Breton - France
        { Languages.CA, "ES" }, // Catalan - Spain
        { Languages.CO, "FR" }, // Corsican - France
        { Languages.CS, "CZ" }, // Czech - Czech Republic
        { Languages.CY, "GB" }, // Welsh - United Kingdom
        { Languages.DA, "DK" }, // Danish - Denmark
        { Languages.DE, "DE" }, // German - Germany
        { Languages.DZ, "BT" }, // Bhutani - Bhutan
        { Languages.EL, "GR" }, // Greek - Greece
        { Languages.EN, "GB" }, // English - United Kingdom
        { Languages.EO, "UN" }, // Esperanto - International
        { Languages.ES, "ES" }, // Spanish - Spain
        { Languages.ET, "EE" }, // Estonian - Estonia
        { Languages.EU, "ES" }, // Basque - Spain
        { Languages.FA, "IR" }, // Persian - Iran
        { Languages.FI, "FI" }, // Finnish - Finland
        { Languages.FJ, "FJ" }, // Fiji - Fiji
        { Languages.FO, "FO" }, // Faeroese - Faroe Islands
        { Languages.FR, "FR" }, // French - France
        { Languages.FY, "NL" }, // Frisian - Netherlands
        { Languages.GA, "IE" }, // Irish - Ireland
        { Languages.GD, "GB" }, // Scots Gaelic - United Kingdom
        { Languages.GL, "ES" }, // Galician - Spain
        { Languages.GN, "PY" }, // Guarani - Paraguay
        { Languages.GU, "IN" }, // Gujarati - India
        { Languages.HA, "NG" }, // Hausa - Nigeria
        { Languages.HI, "IN" }, // Hindi - India
        { Languages.HR, "HR" }, // Croatian - Croatia
        { Languages.HU, "HU" }, // Hungarian - Hungary
        { Languages.HY, "AM" }, // Armenian - Armenia
        { Languages.IA, "UN" }, // Interlingua - International
        { Languages.IE, "UN" }, // Interlingue - International
        { Languages.IK, "US" }, // Inupiak - USA (Alaska)
        { Languages.IN, "ID" }, // Indonesian - Indonesia
        { Languages.IS, "IS" }, // Icelandic - Iceland
        { Languages.IT, "IT" }, // Italian - Italy
        { Languages.IW, "IL" }, // Hebrew - Israel
        { Languages.JA, "JP" }, // Japanese - Japan
        { Languages.JI, "IL" }, // Yiddish - Israel
        { Languages.JW, "ID" }, // Javanese - Indonesia
        { Languages.KA, "GE" }, // Georgian - Georgia
        { Languages.KK, "KZ" }, // Kazakh - Kazakhstan
        { Languages.KL, "GL" }, // Greenlandic - Greenland
        { Languages.KM, "KH" }, // Cambodian - Cambodia
        { Languages.KN, "IN" }, // Kannada - India
        { Languages.KO, "KR" }, // Korean - South Korea
        { Languages.KS, "IN" }, // Kashmiri - India
        { Languages.KU, "TR" }, // Kurdish - Turkey
        { Languages.KY, "KG" }, // Kirghiz - Kyrgyzstan
        { Languages.LA, "VA" }, // Latin - Vatican
        { Languages.LN, "CG" }, // Lingala - Congo
        { Languages.LO, "LA" }, // Laothian - Laos
        { Languages.LT, "LT" }, // Lithuanian - Lithuania
        { Languages.LV, "LV" }, // Latvian - Latvia
        { Languages.MG, "MG" }, // Malagasy - Madagascar
        { Languages.MI, "NZ" }, // Maori - New Zealand
        { Languages.MK, "MK" }, // Macedonian - Macedonia
        { Languages.ML, "IN" }, // Malayalam - India
        { Languages.MN, "MN" }, // Mongolian - Mongolia
        { Languages.MO, "MD" }, // Moldavian - Moldova
        { Languages.MR, "IN" }, // Marathi - India
        { Languages.MS, "MY" }, // Malay - Malaysia
        { Languages.MT, "MT" }, // Maltese - Malta
        { Languages.MY, "MM" }, // Burmese - Myanmar
        { Languages.NA, "NR" }, // Nauru - Nauru
        { Languages.NE, "NP" }, // Nepali - Nepal
        { Languages.NL, "NL" }, // Dutch - Netherlands
        { Languages.NO, "NO" }, // Norwegian - Norway
        { Languages.OC, "FR" }, // Occitan - France
        { Languages.OM, "ET" }, // Oromo - Ethiopia
        { Languages.OR, "IN" }, // Oriya - India
        { Languages.PA, "IN" }, // Punjabi - India
        { Languages.PL, "PL" }, // Polish - Poland
        { Languages.PS, "AF" }, // Pashto - Afghanistan
        { Languages.PT, "PT" }, // Portuguese - Portugal
        { Languages.QU, "PE" }, // Quechua - Peru
        { Languages.RM, "CH" }, // Rhaeto-Romance - Switzerland
        { Languages.RN, "BI" }, // Kirundi - Burundi
        { Languages.RO, "RO" }, // Romanian - Romania
        { Languages.RU, "RU" }, // Russian - Russia
        { Languages.RW, "RW" }, // Kinyarwanda - Rwanda
        { Languages.SA, "IN" }, // Sanskrit - India
        { Languages.SD, "IN" }, // Sindhi - India
        { Languages.SG, "CF" }, // Sangro - Central African Republic
        { Languages.SH, "RS" }, // Serbo-Croatian - Serbia
        { Languages.SI, "LK" }, // Singhalese - Sri Lanka
        { Languages.SK, "SK" }, // Slovak - Slovakia
        { Languages.SL, "SI" }, // Slovenian - Slovenia
        { Languages.SM, "WS" }, // Samoan - Samoa
        { Languages.SN, "ZW" }, // Shona - Zimbabwe
        { Languages.SO, "SO" }, // Somali - Somalia
        { Languages.SQ, "AL" }, // Albanian - Albania
        { Languages.SR, "RS" }, // Serbian - Serbia
        { Languages.SS, "SZ" }, // Siswati - Swaziland
        { Languages.ST, "LS" }, // Sesotho - Lesotho
        { Languages.SU, "SD" }, // Sudanese - Sudan
        { Languages.SV, "SE" }, // Swedish - Sweden
        { Languages.SW, "TZ" }, // Swahili - Tanzania
        { Languages.TA, "IN" }, // Tamil - India
        { Languages.TE, "IN" }, // Telugu - India
        { Languages.TG, "TJ" }, // Tajik - Tajikistan
        { Languages.TH, "TH" }, // Thai - Thailand
        { Languages.TI, "ET" }, // Tigrinya - Ethiopia
        { Languages.TK, "TM" }, // Turkmen - Turkmenistan
        { Languages.TL, "PH" }, // Tagalog - Philippines
        { Languages.TN, "BW" }, // Setswana - Botswana
        { Languages.TO, "TO" }, // Tonga - Tonga
        { Languages.TR, "TR" }, // Turkish - Turkey
        { Languages.TS, "ZA" }, // Tsonga - South Africa
        { Languages.TT, "RU" }, // Tatar - Russia
        { Languages.TW, "GH" }, // Twi - Ghana
        { Languages.UK, "UA" }, // Ukrainian - Ukraine
        { Languages.UR, "PK" }, // Urdu - Pakistan
        { Languages.UZ, "UZ" }, // Uzbek - Uzbekistan
        { Languages.VI, "VN" }, // Vietnamese - Vietnam
        { Languages.VO, "UN" }, // Volapuk - International
        { Languages.WO, "SN" }, // Wolof - Senegal
        { Languages.XH, "ZA" }, // Xhosa - South Africa
        { Languages.YO, "NG" }, // Yoruba - Nigeria
        { Languages.ZH, "CN" }, // Chinese - China
        { Languages.ZU, "ZA" } // Zulu - South Africa
    };

    /// <summary>
    /// Returns ISO 3166-1-alpha-2 code of a given language
    /// </summary>
    /// <param name="language"></param>
    /// <returns></returns>
    public static string GetCountryCodeIso3166Alpha2(Languages language)
    {
        return LanguageCodesIso3166Alpha2.TryGetValue(language, out string? code) ? code : string.Empty;
    }
}
