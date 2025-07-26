using Amazon.CDK;
using Amazon.CDK.AWS.CloudWatch.Actions;
using Amazon.CDK.AWS.DynamoDB;
using Amazon.CDK.AWS.Lambda;
using Amazon.CDK.AWS.S3;
using Amazon.CDK.AWS.SES;
using Amazon.CDK.AWS.SES.Actions;
using Constructs;

namespace Infra;

public class InfraStack : Stack
{
    private string EnvironmentName { get; }
    
    internal InfraStack(Construct scope, string envName, IStackProps props = null) : base(scope, $"JmapMail-{envName}", props)
    {
        EnvironmentName = envName;

        // S3 bucket with unique name per environment
        var mailBucket = new Bucket(this, $"mail-bucket-{EnvironmentName}", new BucketProps
        {
            BucketName = $"mail-bucket-{EnvironmentName}-{Account}-{Region}".ToLower(),
            Versioned = true,
            RemovalPolicy = RemovalPolicy.DESTROY
        });

        // DynamoDB table with unique name per environment
        var metadataTable = new Table(this, $"MailMetadata-{EnvironmentName}", new TableProps
        {
            TableName = $"MailMetadata-{EnvironmentName}",
            PartitionKey = new Attribute
            {
                Name = "MessageId",
                Type = AttributeType.STRING
            },
            BillingMode = BillingMode.PAY_PER_REQUEST,
            RemovalPolicy = RemovalPolicy.DESTROY
        });

        // Lambda for processing messages (placeholder .NET Lambda)
        var processorLambda = new Function(this, $"MailProcessor-{EnvironmentName}", new FunctionProps
        {
            Runtime = Runtime.DOTNET_8,
            Handler = "MailProcessor::MailProcessor.Function::FunctionHandler",
            Code = Code.FromAsset("../lambdas/MailProcessor/src/bin/Release/net8.0/linux-x64/publish"),
            Timeout = Duration.Seconds(30),
            Environment = new System.Collections.Generic.Dictionary<string, string>
            {
                ["BUCKET_NAME"] = mailBucket.BucketName,
                ["TABLE_NAME"] = metadataTable.TableName
            }
        });

        // Grant Lambda permissions
        mailBucket.GrantReadWrite(processorLambda);
        metadataTable.GrantReadWriteData(processorLambda);

        // SES receipt rule set
        var ruleSet = new ReceiptRuleSet(this, $"MailRuleSet-{EnvironmentName}");
        ruleSet.AddRule($"SaveToS3-{EnvironmentName}", new ReceiptRuleOptions
        {
            Recipients = new[] { "fosssauce.com" }, // all email to your domain
            Actions = new IReceiptRuleAction[]
            {
                new S3(new S3Props
                {
                    Bucket = mailBucket,
                    ObjectKeyPrefix = "mail/"
                }),
                new Lambda(new LambdaProps
                {
                    Function = processorLambda
                }),
            },
            Enabled = true
        });
    }
}
