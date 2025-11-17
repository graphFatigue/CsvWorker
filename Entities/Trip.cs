using CsvHelper.Configuration.Attributes;
using System.ComponentModel.DataAnnotations.Schema;

namespace CsvWorker.Entities
{
    public class Trip
    {
        public int Id { get; set; }

        public DateTime PickupDateTime { get; set; }

        public DateTime DropoffDateTime { get; set; }

        public short? PassengerCount { get; set; }

        public decimal? TripDistance { get; set; }

        public string StoreAndFwdFlag { get; set; }

        public int? PuLocationId { get; set; }

        public int? DoLocationId { get; set; }

        public decimal? FareAmount { get; set; }

        public decimal? TipAmount { get; set; }
    }

}
