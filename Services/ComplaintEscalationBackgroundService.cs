using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PublicGrievanceSystem.Data;
using PublicGrievanceSystem.Models;

namespace PublicGrievanceSystem.Services
{
    public class ComplaintEscalationBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ComplaintEscalationBackgroundService> _logger;

        public ComplaintEscalationBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<ComplaintEscalationBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(
            CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckComplaintDeadlinesAsync(stoppingToken);
                }
                catch (OperationCanceledException)
                    when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Error while checking complaint deadlines.");
                }

                try
                {
                    await Task.Delay(
                        TimeSpan.FromMinutes(5),
                        stoppingToken);
                }
                catch (OperationCanceledException)
                    when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }

        private async Task CheckComplaintDeadlinesAsync(
            CancellationToken stoppingToken)
        {
            using var scope = _scopeFactory.CreateScope();

            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            var settings = await context.ComplaintUploadSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(stoppingToken);

            if (settings == null ||
                !settings.EnableAutomaticOverdueDetection)
            {
                return;
            }

            var currentTime = DateTime.Now;

            var overdueComplaints = await context.Complaints
                .Where(c =>
                    c.ResolutionDeadline.HasValue &&
                    c.ResolutionDeadline.Value <= currentTime &&
                    c.Status != "Resolved")
                .ToListAsync(stoppingToken);

            foreach (var complaint in overdueComplaints)
            {
                // Record the overdue event only once.
                if (!complaint.OverdueAt.HasValue)
                {
                    complaint.OverdueAt = currentTime;

                    context.ComplaintStatusHistories.Add(
                        new ComplaintStatusHistory
                        {
                            ComplaintId = complaint.Id,
                            Status = complaint.Status,
                            Department = complaint.Department,
                            Priority = complaint.Priority,
                            ChangedAt = currentTime,
                            Remarks =
                                "Resolution deadline has passed. " +
                                "This complaint is now overdue."
                        });
                }

                // Escalate only after the additional configured period.
                if (settings.EnableEscalationNotifications &&
    complaint.ResolutionDeadline is DateTime resolutionDeadline &&
    !complaint.EscalatedAt.HasValue &&
    currentTime >= resolutionDeadline
        .AddHours(settings.EscalationAfterDeadlineHours))
                {
                    complaint.EscalatedAt = currentTime;

                    context.ComplaintStatusHistories.Add(
                        new ComplaintStatusHistory
                        {
                            ComplaintId = complaint.Id,
                            Status = complaint.Status,
                            Department = complaint.Department,
                            Priority = complaint.Priority,
                            ChangedAt = currentTime,
                            Remarks =
                                "Complaint automatically escalated " +
                                "because it remained unresolved beyond " +
                                "the escalation deadline."
                        });
                }
            }

            await context.SaveChangesAsync(stoppingToken);
        }
    }
}