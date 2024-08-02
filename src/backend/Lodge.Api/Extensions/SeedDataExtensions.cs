using Bogus;
using Dapper;
using Lodge.Application.Abstractions.Data;
using Lodge.Application.Abstractions.Storage;
using Lodge.Domain.Apartements;
using System.Data;

namespace Lodge.Api.Extensions;

/// <summary>
/// Contains extensions for the seeding the database.
/// </summary>
public static class SeedDataExtensions
{
    private static readonly List<Guid> ImageIds =
    [
        Guid.Parse("2d3dfec6-3c14-4203-9103-77aa6302aa87"),
        Guid.Parse("3ca997a7-17ee-4711-b357-3f737cefce10"),
        Guid.Parse("6ec5ecb3-15ab-46d4-bf9a-4fcced306e22"),
        Guid.Parse("43e0f96d-6f5a-4af8-ba40-aadb2a36c440"),
        Guid.Parse("4718d083-a3d4-4fd0-b7f6-2b359520301e"),
        Guid.Parse("46278444-c7cd-4e25-85c2-e75977334238"),
        Guid.Parse("b610ae0b-30dd-446a-a6a9-7c825d95b9df"),
        Guid.Parse("c881da3b-26a9-4503-8438-3036bc43d2a0"),
        Guid.Parse("d7cdec23-4232-4887-acfb-64697441f217"),
        Guid.Parse("e3ec9540-c9c0-4127-af86-20b58fa0dec1")
    ];

    /// <summary>
    /// Seeds the images.
    /// </summary>
    /// <param name="app">The application builder.</param>
    public async static Task SeedImages(this IApplicationBuilder app)
    {
        using IServiceScope scope = app.ApplicationServices.CreateScope();

        var webHostEnvironment = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();

        string imagesFolderPath = Path.Combine(webHostEnvironment.WebRootPath, "images");

        string[] imageFiles = Directory.GetFiles(imagesFolderPath);

        var blobService = scope.ServiceProvider.GetRequiredService<IBlobService>();

        foreach (string imageFilePath in imageFiles)
        {
            using FileStream fileStream = new(imageFilePath, FileMode.Open, FileAccess.Read);

            string contentType = GetContentType(imageFilePath);

            Guid fileId = await blobService.UploadAsync(Guid.NewGuid(), fileStream, contentType);

            Console.WriteLine($"Uploaded {Path.GetFileName(imageFilePath)} to blob with ID: {fileId}");
        }
    }

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
                ImageId = faker.PickRandom(ImageIds),
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
                image_id,
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
                    @ImageId,
                    @MaximumGuestCount,
                    @MaximumRoomCount);
            """;

        connection.Execute(sql, apartments);
    }

    /// <summary>
    /// Gets the file content type.
    /// </summary>
    /// <param name="path">The file path.</param>
    /// <returns>The content type.</returns>
    private static string GetContentType(string path)
    {
        string extension = Path.GetExtension(path).ToLowerInvariant();

        return extension switch
        {
            ".jpg" => "image/jpeg",
            ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            _ => "application/octet-stream",
        };
    }
}
