using StartEvent_API.Data.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

namespace StartEvent_API.Data.Seeders
{
    public class VenueSeeder
    {
        public static async Task SeedVenues(IServiceProvider serviceProvider)
        {
            var dbContext = serviceProvider.GetRequiredService<ApplicationDbContext>();

            // Check if venues already exist
            if (await dbContext.Venues.AnyAsync())
            {
                return; // Already seeded
            }

            var venues = new List<Venue>
            {
                new Venue
                {
                    Id = Guid.NewGuid(),
                    Name = "Nelum Pokuna Mahinda Rajapaksa Theatre",
                    Location = "Colombo",
                    Capacity = 1500
                },
                new Venue
                {
                    Id = Guid.NewGuid(),
                    Name = "Bandaranaike Memorial International Conference Hall",
                    Location = "Colombo",
                    Capacity = 2000
                },
                new Venue
                {
                    Id = Guid.NewGuid(),
                    Name = "Kandy City Center Auditorium",
                    Location = "Kandy",
                    Capacity = 800
                },
                new Venue
                {
                    Id = Guid.NewGuid(),
                    Name = "Galle International Cricket Stadium Conference Hall",
                    Location = "Galle",
                    Capacity = 1200
                },
                new Venue
                {
                    Id = Guid.NewGuid(),
                    Name = "Jaffna Cultural Hall",
                    Location = "Jaffna",
                    Capacity = 500
                }
            };

            await dbContext.Venues.AddRangeAsync(venues);
            await dbContext.SaveChangesAsync();

            Console.WriteLine("Sri Lankan venues seeded successfully.");
        }
    }
}
