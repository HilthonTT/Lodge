using Bogus;
using Dapper;
using Lodge.Application.Abstractions.Data;
using Lodge.Domain.Apartements;
using System.Data;

namespace Lodge.Api.Extensions;

/// <summary>
/// Contains extensions methods for the seeding the database.
/// </summary>
public static class SeedDataExtensions
{
    private static readonly List<string> ImageUrls =
    [
        "https://images.unsplash.com/photo-1572120360610-d971b9d7767c?q=80&w=2070&auto=format&fit=crop&ixlib=rb-4.0.3&ixid=M3wxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8fA%3D%3D",
        "https://images.unsplash.com/photo-1591474200742-8e512e6f98f8?ixlib=rb-1.2.1&q=80&fm=jpg&crop=entropy&cs=tinysrgb&w=1080&fit=max",
        "https://images.unsplash.com/photo-1560170412-0f7df0eb0fb1?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&ixid=MnwxMjA3fDB8MXxzZWFyY2h8NHx8aG91c2UlMjBleHRlcmlvcnx8MHx8fHwxNjI4NDM2MzA4&ixlib=rb-1.2.1&q=80&w=1080",
        "https://images.unsplash.com/photo-1558036117-15d82a90b9b1?ixlib=rb-1.2.1&q=80&fm=jpg&crop=entropy&cs=tinysrgb&w=1080&fit=max&ixid=eyJhcHBfaWQiOjEyMDd9",
        "https://images.unsplash.com/photo-1600585153490-76fb20a32601?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&ixid=MnwxMjA3fDB8MXxzZWFyY2h8NHx8cHJvcGVydGllc3x8MHx8fHwxNjI4ODIzMzQw&ixlib=rb-1.2.1&q=80&w=1080",
        "https://images.unsplash.com/photo-1611582785023-91a3c0999d93?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&ixid=MnwxMjA3fDB8MXxzZWFyY2h8M3x8aWdsb298fDB8fHx8MTYxODk3NDEzNw&ixlib=rb-1.2.1&q=80&w=1080",
        "https://images.unsplash.com/photo-1505843513577-22bb7d21e455?ixlib=rb-1.2.1&ixid=eyJhcHBfaWQiOjEyMDd9&w=1000&q=80",
        "https://www.thesprucecrafts.com/thmb/ilwYm_5YAb-Mq83tb_TJ_NMRCBA=/2121x1414/filters:no_upscale():max_bytes(150000):strip_icc()/GettyImages-980431586-4febe9d7191241e2953abf9f0d10eb01.jpg",
        "https://images.unsplash.com/photo-1571168538867-ad36fe110cc4?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&ixid=MnwxMjA3fDB8MXxzZWFyY2h8NHx8aG91c2UlMjBuaWdodHx8MHx8fHwxNjI4Nzc0NTMx&ixlib=rb-1.2.1&q=80&w=1080",
        "https://images.unsplash.com/photo-1515263487990-61b07816b324?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&ixid=MnwxMjA3fDB8MXxzZWFyY2h8M3x8YXBhcnRtZW50fHwwfHx8fDE2MjgzNTI3MTE&ixlib=rb-1.2.1&q=80&w=1080",

    ];

    /// <summary>
    /// Seeds the database.
    /// </summary>
    /// <param name="app">The application builder.</param>
    public static void SeedData(this IApplicationBuilder app)
    {
        using IServiceScope scope = app.ApplicationServices.CreateScope();

        var dbConnectionFactory = scope.ServiceProvider.GetRequiredService<IDbConnectionFactory>();
        using IDbConnection connection = dbConnectionFactory.GetOpenConnection();

        var faker = new Faker();

        List<object> apartments = [];
        List<int> allAmenities = Enum.GetValues(typeof(Amenity)).Cast<int>().ToList();

        for (var i = 0; i < 100; i++)
        {
            int numOfAmenities = faker.Random.Int(1, allAmenities.Count);

            List<int> randomAmenities = faker.PickRandom(allAmenities, numOfAmenities).ToList();
            apartments.Add(new
            {
                Id = Guid.NewGuid(),
                Name = faker.Company.CompanyName(),
                Description = faker.Lorem.Sentences(),
                Country = faker.Address.Country(),
                State = faker.Address.State(),
                ZipCode = faker.Address.ZipCode(),
                City = faker.Address.City(),
                Street = faker.Address.StreetAddress(),
                PriceAmount = faker.Random.Decimal(50, 1000),
                ImageUrl = faker.PickRandom(ImageUrls),
                PriceCurrency = "USD",
                CleaningFeeAmount = faker.Random.Decimal(25, 200),
                CleaningFeeCurrency = "USD",
                MaximumGuestCount = faker.Random.Decimal(1, 10),
                MaximumRoomCount = faker.Random.Decimal(1, 10),
                Amenities = randomAmenities,
                LastBookedOn = DateTime.MinValue,
                CreatedOnUtc = DateTime.UtcNow,
            });
        }

        const string sql =
            """
                INSERT INTO apartments
                (id, 
                name, 
                description, 
                address_country, 
                address_state, 
                address_zip_code, 
                address_city, 
                address_street,
                price_amount, 
                price_currency, 
                cleaning_fee_amount,
                cleaning_fee_currency, 
                amenities, 
                last_booked_on_utc, 
                created_on_utc,
                image_url,
                maximum_guest_count,
                maximum_room_count)
                VALUES(
                    @Id, 
                    @Name,
                    @Description,
                    @Country,
                    @State, 
                    @ZipCode,
                    @City,
                    @Street, 
                    @PriceAmount, 
                    @PriceCurrency, 
                    @CleaningFeeAmount,
                    @CleaningFeeCurrency, 
                    @Amenities,
                    @LastBookedOn,
                    @CreatedOnUtc,
                    @ImageUrl,
                    @MaximumGuestCount,
                    @MaximumRoomCount);
            """;

        connection.Execute(sql, apartments);
    }
}
