using System;
using System.Collections.Generic;
using HotelBooking.Core;
using HotelBooking.UnitTests.Fakes;
using Xunit;
using System.Linq;
using System.Threading.Tasks;
using Moq;

namespace HotelBooking.UnitTests
{
    public class BookingManagerTests
    {
        private IBookingManager bookingManager;
        IRepository<Booking> bookingRepository;

        private readonly BookingManager _ownBookingManager;
        private readonly Mock<IRepository<Booking>> _mockBookingRepository;
        private readonly Mock<IRepository<Room>> _mockRoomRepository;

        public BookingManagerTests()
        {
            DateTime start = DateTime.Today.AddDays(10);
            DateTime end = DateTime.Today.AddDays(20);
            bookingRepository = new FakeBookingRepository(start, end);
            IRepository<Room> roomRepository = new FakeRoomRepository();
            bookingManager = new BookingManager(bookingRepository, roomRepository);

            _mockBookingRepository = new Mock<IRepository<Booking>>();
            _mockRoomRepository = new Mock<IRepository<Room>>();

            _ownBookingManager = new BookingManager(_mockBookingRepository.Object, _mockRoomRepository.Object);
        }

        [Fact]
        public async Task FindAvailableRoom_StartDateNotInTheFuture_ThrowsArgumentException()
        {
            // Arrange
            DateTime date = DateTime.Today;

            // Act
            Task result() => bookingManager.FindAvailableRoom(date, date);

            // Assert
            await Assert.ThrowsAsync<ArgumentException>(result);
        }

        [Fact]
        public async Task FindAvailableRoom_RoomAvailable_RoomIdNotMinusOne()
        {
            // Arrange
            DateTime date = DateTime.Today.AddDays(1);
            // Act
            int roomId = await bookingManager.FindAvailableRoom(date, date);
            // Assert
            Assert.NotEqual(-1, roomId);
        }

        [Fact]
        public async Task FindAvailableRoom_RoomAvailable_ReturnsAvailableRoom()
        {
            // This test was added to satisfy the following test design
            // principle: "Tests should have strong assertions".

            // Arrange
            DateTime date = DateTime.Today.AddDays(1);

            // Act
            int roomId = await bookingManager.FindAvailableRoom(date, date);

            var bookingForReturnedRoomId = (await bookingRepository.GetAllAsync()).Where(b => b.RoomId == roomId
                && b.StartDate <= date
                && b.EndDate >= date
                && b.IsActive);

            // Assert
            Assert.Empty(bookingForReturnedRoomId);
        }

/////////////////////////////////////////////////////////////////////////////////////////////////////////////

        [Theory]
        [InlineData("2025-05-10", "2025-05-15")] // Valid input data
        [InlineData("2025-05-10", "2025-05-10")] // Same day Booking
        public async Task FindAvailableRoom_ShouldReturnValidRoomId(string start, string end)
        {
            // Arrange
            DateTime startDate = DateTime.Parse(start);
            DateTime endDate = DateTime.Parse(end);

            _mockRoomRepository.Setup(repo => repo.GetAllAsync()).ReturnsAsync(new List<Room>
            {
                new Room { Id = 1, Description = "Room A" },
                new Room { Id = 2, Description = "Room B" }
            });

            _mockBookingRepository.Setup(repo => repo.GetAllAsync()).ReturnsAsync(new List<Booking>
            {
                new Booking
                {
                    RoomId = 1, StartDate = DateTime.Parse("2025-05-05"), EndDate = DateTime.Parse("2025-05-09"),
                    IsActive = true
                }
            });

            // Act
            int result = await _ownBookingManager.FindAvailableRoom(startDate, endDate);

            // Assert
            Assert.True(result >= 1 || result == -1, "Room ID should be valid or -1 if unavailable.");

        }

        [Theory]
        [InlineData("2025-01-10", "2025-05-15")] // Start date is in the past
        [InlineData("2025-02-15", "2024-02-14")] // Startdate > Enddate
        public async Task FindavailableRoom_InvalidDates_ShouldThrowArguementException(string start, string end)
        {
            // Arrange
            DateTime startDate = DateTime.Parse(start);
            DateTime endDate = DateTime.Parse(end);

            // Act

            // Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _ownBookingManager.FindAvailableRoom(startDate, endDate));
        }

        public static IEnumerable<object[]> FullyOccupiedDateTestData()
        {
            yield return new object[] { "2025-05-01", "2025-05-10", new List<string> { "2025-05-05", "2025-05-06" } };
            yield return new object[] { "2025-06-01", "2025-06-05", new List<string> { } };
        }

        [Theory]
        [MemberData(nameof(FullyOccupiedDateTestData))]
        public async Task GetFullyOccupiedDates_ShouldReturnCorrectDates_Dates(string start, string end,
            List<string> expectedDates)
        {
            // Arrange
            DateTime startDate = DateTime.Parse(start);
            DateTime endDate = DateTime.Parse(end);

            var bookings = new List<Booking>
            {
                new Booking
                {
                    RoomId = 1, StartDate = DateTime.Parse("2025-05-05"), EndDate = DateTime.Parse("2025-05-06"),
                    IsActive = true
                },
                new Booking
                {
                    RoomId = 2, StartDate = DateTime.Parse("2025-05-05"), EndDate = DateTime.Parse("2025-05-06"),
                    IsActive = true
                }
            };
            
            _mockBookingRepository.Setup(repo => repo.GetAllAsync()).ReturnsAsync(bookings);
            _mockRoomRepository.Setup(repo => repo.GetAllAsync()).ReturnsAsync(new List<Room> {new Room {Id = 1}, new Room {Id = 2}});
            
            // Act
            var result = await _ownBookingManager.GetFullyOccupiedDates(startDate, endDate);
            
            // Assert
            var expected = expectedDates.Select(DateTime.Parse).ToList();
            Assert.Equal(expected, result);
        }

        [Theory]
        [ClassData(typeof(BookingTestData))]
        public async Task CreateBookingAsync_ShouldReturnCorrectResults_UnOrAvailableRooms(DateTime start,
            DateTime end, bool expected)
        {
            //Arrange
            var booking = new Booking { StartDate = start, EndDate = end };

            _mockBookingRepository.Setup(repo => repo.AddAsync(It.IsAny<Booking>())).Returns(Task.CompletedTask);
            _mockBookingRepository.Setup(repo => repo.GetAllAsync()).ReturnsAsync(new List<Booking>());
            
            // Simulate no avail rooms when expected = false
            if (!expected)
            {
                _mockRoomRepository.Setup(repo => repo.GetAllAsync()).ReturnsAsync(new List<Room>()); // Empty list
            }
            else
            {
                _mockRoomRepository.Setup(repo => repo.GetAllAsync()).ReturnsAsync(new List<Room>{new Room{Id = 1}});
            }
            
            // Act
            bool result = await _ownBookingManager.CreateBooking(booking);
            
            // Assert
            Assert.Equal(expected, result);
        }
    }
}
