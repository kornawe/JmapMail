using Amazon.CDK;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Infra;

sealed class Program
{
    public static void Main(string[] args)
    {
        var app = new App();

        var envName = (string)app.Node.TryGetContext("env") ?? "dev";

        // Get environment settings from cdk.json
        var environments = (Dictionary<string, object>)app.Node.TryGetContext("environments");
        var envConfig = (Dictionary<string, object>)environments[envName];

        var awsEnv = new Amazon.CDK.Environment
        {
            Account = envConfig["account"].ToString(),
            Region = envConfig["region"].ToString()
        };

        new InfraStack(app, envName, new StackProps
        {
            Env = awsEnv,
            Description = $"MailServer infrastructure for {envName}"
        });
        app.Synth();
    }
}
