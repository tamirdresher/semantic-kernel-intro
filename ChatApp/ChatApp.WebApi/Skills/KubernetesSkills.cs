using System.ComponentModel;
using Microsoft.SemanticKernel;
using System.Diagnostics;
using k8s;
using System.Text;

namespace ChatApp.WebApi.Skills
{
    class KuberenetesSkills
    {
        private readonly ILogger<KuberenetesSkills> _logger;

        public KuberenetesSkills(ILogger<KuberenetesSkills> logger)
        {
            _logger = logger;
        }

        [Description(
            "kubectl runner for read-only command, can run kubectl commands but only if they are for reading info, just provide the arguments to the kubectl and it will run it and return the result as string")]
        [KernelFunction()]
        public async Task<string> RunKubectl(string kubectlArguements)
        {
            Console.WriteLine($"{kubectlArguements}");

            var outputBuilder = new StringBuilder();

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "kubectl",
                    Arguments = kubectlArguements,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    outputBuilder.AppendLine(e.Data);
                }
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    outputBuilder.AppendLine(e.Data); // Include error in output
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync();
            var output = outputBuilder.ToString();
            _logger.LogInformation("{kubectlArgs} {kubectlResult}", kubectlArguements, output); 
            return output;

            //return "No resources found in default namespace.";
        }
    }
}
