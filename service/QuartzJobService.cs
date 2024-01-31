using Quartz;
using ddns.net.job;

namespace ddns.net.service
{
    public class QuartzJobService
    {
        private readonly ISchedulerFactory _schedulerFactory;

        public QuartzJobService(ISchedulerFactory schedulerFactory)
        {
            _schedulerFactory = schedulerFactory;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        public  async Task<bool> StartDDNSJob(string expression)
        {
            try
            {
                var __scheduler1 = await _schedulerFactory.GetScheduler();
                var jobKey = new JobKey("ddns.job.key", "group1");
                var triggerKey = new TriggerKey("ddns.trigger.key", "group1");

                //var jobBuilder = JobBuilder.Create<DDNSJob>();

                //__scheduler1.AddJob<DDNSJob>(opts => opts.WithIdentity(jobKey));

                var jobDetail = await __scheduler1.GetJobDetail(jobKey);
                if (jobDetail != null)
                {
                    //await __scheduler1.Shutdown();
                    await __scheduler1.DeleteJob(jobKey);//删掉原来的
                }

                // define the job and tie it to our DDNSJob class
                IJobDetail job = JobBuilder.Create<AutoDDNSJob>()
                 .WithIdentity(jobKey)
                 .Build();
                ITrigger trigger;
                //var expression = Appsettings.Get("Interval");
                if (CronExpression.IsValidExpression(expression))
                {
                    trigger = TriggerBuilder.Create()
                     .WithIdentity(triggerKey)
                     .WithCronSchedule(expression)
                     .StartAt(DateTime.Now.AddSeconds(30))
                     .StartNow()
                     .Build();
                }
                else
                {
                    int Interval = int.TryParse(expression, out int _Interval) ? _Interval : 30;
                    trigger = TriggerBuilder.Create()
                   .WithIdentity(triggerKey)
                   //.StartNow()
                   .StartAt(DateTime.Now.AddSeconds(30))
                   .WithSimpleSchedule(x => x
                    .WithIntervalInMinutes(Interval)
                    .RepeatForever())
                   .Build();
                }
                // Tell Quartz to schedule the job using our trigger
                await __scheduler1.ScheduleJob(job, trigger);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error("start ddns job error", ex);
                return false;
            }
            return true;
        }
    }
}
