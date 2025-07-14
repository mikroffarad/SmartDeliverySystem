using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartDeliverySystem.Controllers;
using SmartDeliverySystem.Models;
using SmartDeliverySystem.Data;
using Xunit;
using FluentAssertions;

namespace SmartDeliverySystem.Tests.Controllers
{
    public class VendorControllerTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly VendorController _controller;

        public VendorControllerTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _controller = new VendorController(_context);
        }

        [Fact]
        public async Task GetAllVendors_ReturnsEmptyList_WhenNoVendorsExist()
        {
            // Act
            var result = await _controller.GetAllVendors();

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetAllVendors_ReturnsVendorsList_WhenVendorsExist()
        {
            // Arrange
            var vendors = new List<Vendor>
            {
                new Vendor { Name = "Vendor 1", Latitude = 50.0, Longitude = 30.0 },
                new Vendor { Name = "Vendor 2", Latitude = 51.0, Longitude = 31.0 }
            };

            _context.Vendors.AddRange(vendors);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.GetAllVendors();

            // Assert
            result.Should().HaveCount(2);
            result.Should().Contain(v => v.Name == "Vendor 1");
            result.Should().Contain(v => v.Name == "Vendor 2");
        }

        [Fact]
        public async Task GetVendor_ReturnsVendor_WhenVendorExists()
        {
            // Arrange
            var vendor = new Vendor { Name = "Test Vendor", Latitude = 50.0, Longitude = 30.0 };
            _context.Vendors.Add(vendor);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.GetVendor(vendor.Id);

            // Assert
            var actionResult = result.Result as OkObjectResult;
            actionResult.Should().NotBeNull();

            var returnedVendor = actionResult.Value as Vendor;
            returnedVendor.Should().NotBeNull();
            returnedVendor.Name.Should().Be("Test Vendor");
        }

        [Fact]
        public async Task GetVendor_ReturnsNotFound_WhenVendorDoesNotExist()
        {
            // Act
            var result = await _controller.GetVendor(999);

            // Assert
            result.Result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task CreateVendor_ReturnsCreatedVendor_WhenValidData()
        {
            // Arrange
            var vendorData = new { Name = "New Vendor", Latitude = 50.5, Longitude = 30.5 };

            // Act
            var result = await _controller.CreateVendor(vendorData);

            // Assert
            var actionResult = result.Result as CreatedAtActionResult;
            actionResult.Should().NotBeNull();
            var createdVendor = actionResult.Value as Vendor;
            createdVendor.Should().NotBeNull();
            createdVendor!.Name.Should().Be("New Vendor");

            // Verify it's in database
            var vendorInDb = await _context.Vendors.FindAsync(createdVendor.Id);
            vendorInDb.Should().NotBeNull();
        }

        [Fact]
        public async Task UpdateVendor_ReturnsUpdatedVendor_WhenVendorExists()
        {
            // Arrange
            var vendor = new Vendor { Name = "Original Name", Latitude = 50.0, Longitude = 30.0 };
            _context.Vendors.Add(vendor);
            await _context.SaveChangesAsync();

            var updateData = new { Name = "Updated Name", Latitude = 51.0, Longitude = 31.0 };

            // Act
            var result = await _controller.UpdateVendor(vendor.Id, updateData);

            // Assert
            var actionResult = result.Result as OkObjectResult;
            actionResult.Should().NotBeNull();
            var updatedVendor = actionResult.Value as Vendor;
            updatedVendor.Should().NotBeNull();
            updatedVendor!.Name.Should().Be("Updated Name");
        }

        [Fact]
        public async Task DeleteVendor_ReturnsNoContent_WhenVendorExists()
        {
            // Arrange
            var vendor = new Vendor { Name = "To Delete", Latitude = 50.0, Longitude = 30.0 };
            _context.Vendors.Add(vendor);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.DeleteVendor(vendor.Id);

            // Assert
            result.Should().BeOfType<NoContentResult>();

            // Verify it's deleted from database
            var deletedVendor = await _context.Vendors.FindAsync(vendor.Id);
            deletedVendor.Should().BeNull();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
