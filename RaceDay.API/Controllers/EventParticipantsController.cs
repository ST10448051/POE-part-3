
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using System.Security.Claims;

namespace RaceDay.API.Controllers;

[ApiController]
[Route("api/events/{eventId:int}/participants")]
[Authorize(Roles = "Organiser")]
public class EventParticipantsController : ControllerBase
{
    private readonly RaceDayDbContext _context;

    public EventParticipantsController(RaceDayDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetEventParticipants(
        int eventId)
    {
        var userIdValue = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdValue, out var organiserId))
            return Unauthorized();

        // Check that the event exists and belongs
        // to the currently authenticated Organiser.
        var raceEvent = await _context.Events
            .AsNoTracking()
            .Where(e =>
                e.EventID == eventId &&
                e.OrganiserID == organiserId)
            .Select(e => new
            {
                e.EventID,
                e.EventName
            })
            .FirstOrDefaultAsync();

        if (raceEvent == null)
            return NotFound(new
            {
                message = "Event not found or access denied."
            });

        // Retrieve participants registered for this event.
        var participants = await _context.Enrolments
            .AsNoTracking()
            .Where(e =>
                e.EventCategory.EventID == eventId)
            .OrderBy(e => e.Participant.LastName)
            .ThenBy(e => e.Participant.FirstName)
            .Select(e => new
            {
                e.EnrolmentID,
                e.ParticipantID,

                e.Participant.FirstName,
                e.Participant.LastName,
                e.Participant.Email,

                e.EventCategoryID,
                e.EventCategory.CategoryID,
                e.EventCategory.Category.CategoryName,

                e.EnrolmentDate,
                e.Status
            })
            .ToListAsync();

        // Calculate enrolment statistics.
        var totalEnrolments = participants.Count;

        var confirmed = participants.Count(
            p => p.Status == "Confirmed");

        var pending = participants.Count(
            p => p.Status == "Pending");

        var cancelled = participants.Count(
            p => p.Status == "Cancelled");

        var completed = participants.Count(
            p => p.Status == "Completed");

        return Ok(new
        {
            raceEvent.EventID,
            raceEvent.EventName,

            TotalEnrolments = totalEnrolments,
            Confirmed = confirmed,
            Pending = pending,
            Cancelled = cancelled,
            Completed = completed,

            Participants = participants
        });
    }
}
