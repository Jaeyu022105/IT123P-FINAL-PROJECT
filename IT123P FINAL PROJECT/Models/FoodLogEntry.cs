using SQLite;

namespace IT123P_FINAL_PROJECT.Models
{
    public class FoodLogEntry
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        /// <summary>The corresponding ID from the backend server database. 0 = not yet synced.</summary>
        public int ServerId { get; set; }

        /// <summary>True once the entry has been successfully pushed to the backend.</summary>
        public bool IsSynced { get; set; }

        [Indexed]
        public string FoodName { get; set; } = string.Empty;

        public double Calories { get; set; }
        public double Protein { get; set; } // in grams
        public double Carbs { get; set; }   // in grams
        public double Fat { get; set; }     // in grams
        public double Grams { get; set; }

        public DateTime DateLogged { get; set; }
    }
}
