namespace Zo.Data
{
    public class Tier
    {
        #region Contants

        public static readonly Tier[] HIERARCHY = new[]
        {
            // minor nobility
            new Tier(05, "ministerial", "ministerials", "ministerial", "ministerialship", "ministerialships", "ministerialship", "ministerial"), // unfree noble. not hereditary
            new Tier(10, "knight", "knights", "knightess", "knightdom", "knightdoms", "knightship", "knightly"),
            new Tier(15, "baronet", "baronets", "baronetess", "baronetcy", "baronetcies", "baronetcy", "baronetical"), // landless baron

            // low nobility
            new Tier(20, "baron", "barons", "baroness", "barony", "baronies",  "baronship", "baronial"),
            new Tier(25, "viscount", "viscounts", "viscountess", "viscounty", "viscounties",  "viscountship", "viscomital"),
            new Tier(30, "count", "counts", "countess", "county", "counties",  "countship", "comital"), // first real landed lord with landed subjects

            // middle nobility
            new Tier(35, "palatine", "palatines", "palatiness", "palatinate", "palatinates", "palatineship", "palatinal"), // count with royal blessings
            new Tier(40, "gees", "geeses", "gioness", "geesing", "geesings",  "geeship", "geesical"),
            new Tier(45, "marquis", "marquises", "marchioness", "marquisate", "marquisates",  "marquiship", "marquisical"),

            // high nobility
            new Tier(50, "duke", "dukes", "duchess", "duchy", "duchies",  "dukeship", "ducal"), // 'your highness' starts from here
            new Tier(55, "prince", "princes", "princess", "principality", "principalities", "princeship", "princely"), // prince (independent ruler) is not royalty. prince (relative of king/emperor) is royalty. any independent duke or hern is a prince of a principality
            new Tier(60, "hern", "herns", "hernissa", "herning", "hernings",  "hernship", "hernal"),

            // royalty
            new Tier(65, "viceroy", "viceroys", "vicereine", "viceroyalty", "viceroyalties",  "viceroyship", "viceregal"),
            new Tier(70, "king", "kings", "queen", "kingdom", "kingdoms",  "kingship", "royal"),
            new Tier(75, "emperor", "emperors", "empress", "empire", "empires",  "empireship", "imperial"),
        };

        #endregion

        #region Constructors

        public Tier(int rank, string title, string titlePlural, string titleFemale,
            string realm, string realmPlural, string fief, string adjective)
        {
            this.Rank = rank;
            this.Title = title;
            this.TitlePlural = titlePlural;
            this.TitleFemale = titleFemale;
            this.Realm = realm;
            this.RealmPlural = realmPlural;
            this.Fief = fief;
            this.Adjective = adjective;
        }

        #endregion

        #region Properties

        public int Rank { get; protected set; }

        public string Title { get; protected set; }

        public string TitlePlural { get; protected set; }

        public string TitleFemale { get; protected set; }

        public string Realm { get; protected set; }

        public string RealmPlural { get; protected set; }

        public string Fief { get; protected set; }

        public string Adjective { get; protected set; }

        #endregion
    }
}