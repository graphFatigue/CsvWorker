using CsvWorker.Entities;
using Microsoft.EntityFrameworkCore;

namespace CsvWorker.DAL
{
    public class AppDbContext: DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) 
            : base(options)
        {
            
        }

        public DbSet<Trip> Trips { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var e = modelBuilder.Entity<Trip>();

            e.ToTable("trips", "dbo");

            e.HasKey(x => x.Id);
            e.Property(x => x.Id)
             .HasColumnName("ID")
             .ValueGeneratedOnAdd();

            e.Property(x => x.PickupDateTime)
             .HasColumnName("tpep_pickup_datetime")
             .HasColumnType("datetime2(3)")
             .IsRequired();

            e.Property(x => x.DropoffDateTime)
             .HasColumnName("tpep_dropoff_datetime")
             .HasColumnType("datetime2(3)")
             .IsRequired();

            e.Property(x => x.PassengerCount)
             .HasColumnName("passenger_count")
             .HasColumnType("smallint");

            e.Property(x => x.TripDistance)
             .HasColumnName("trip_distance")
             .HasColumnType("decimal(6,2)");

            e.Property(x => x.StoreAndFwdFlag)
             .HasColumnName("store_and_fwd_flag")
             .HasColumnType("varchar(3)");

            e.Property(x => x.FareAmount)
             .HasColumnName("fare_amount")
             .HasColumnType("decimal(10,2)");

            e.Property(x => x.TipAmount)
             .HasColumnName("tip_amount")
             .HasColumnType("decimal(10,2)");

            e.Property(x => x.PuLocationId)
             .HasColumnName("PULocationID")
             .HasColumnType("int");

            e.Property(x => x.DoLocationId)
             .HasColumnName("DOLocationID")
             .HasColumnType("int");

            e.Property(x => x.TripDurationSeconds)
            .HasColumnName("trip_duration_seconds")
            .HasComputedColumnSql("DATEDIFF(SECOND, tpep_pickup_datetime, tpep_dropoff_datetime)", stored: true);

            e.HasIndex(x => x.PuLocationId).HasDatabaseName("IX_trips_PULocationID");
            e.HasIndex(x => x.TripDistance).HasDatabaseName("IX_trips_TripDistance");
            e.HasIndex(x => x.TripDurationSeconds).HasDatabaseName("IX_trips_TripDurationSeconds");

            base.OnModelCreating(modelBuilder);
        }
    }
}
