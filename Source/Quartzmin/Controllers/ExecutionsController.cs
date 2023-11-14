namespace Quartzmin.Controllers;

public class ExecutionsController : PageControllerBase
{
    [HttpGet]
    public async Task<IActionResult> IndexAsync()
    {
        var currentlyExecutingJobs = await Scheduler.GetCurrentlyExecutingJobs();

        var list = new List<object>();

        foreach (var exec in currentlyExecutingJobs)
        {
            list.Add(new
            {
                Id = exec.FireInstanceId,
                JobGroup = exec.JobDetail.Key.Group,
                JobName = exec.JobDetail.Key.Name,
                TriggerGroup = exec.Trigger.Key.Group,
                TriggerName = exec.Trigger.Key.Name,
                ScheduledFireTime = exec.ScheduledFireTimeUtc?.UtcDateTime.ToDefaultFormat(),
                ActualFireTime = exec.FireTimeUtc.UtcDateTime.ToDefaultFormat(),
                RunTime = exec.JobRunTime.ToString("hh\\:mm\\:ss")
            });
        }

        return View(list);
    }

    public class InterruptArgs
    {
        public string Id { get; set; }
    }

    [HttpPost, JsonErrorResponse]
    public async Task<IActionResult> InterruptAsync([FromBody] InterruptArgs args)
    {
        if (!await Scheduler.Interrupt(args.Id))
        {
            throw new InvalidOperationException("Cannot interrupt execution " + args.Id);
        }

        return NoContent();
    }
}