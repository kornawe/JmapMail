using Amazon.CDK;
using Constructs;

namespace Infra;

public class InfraStack : Stack
{
    private string EnvironmentName { get; }
    
    internal InfraStack(Construct scope, string envName, IStackProps props = null) : base(scope, $"JmapMail-{envName}", props)
    {
        EnvironmentName = envName;

        // S3 bucket with unique name per environment
        var bucket = new Bucket(this, $"mail-bucket-{EnvironmentName}", new BucketProps
        {
            BucketName = $"mail-bucket-{EnvironmentName}-{Account}-{Region}".ToLower(),
            Versioned = true,
            RemovalPolicy = RemovalPolicy.DESTROY
        });

        // DynamoDB table with unique name per environment
        new Table(this, $"MailMetadata-{EnvironmentName}", new TableProps
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

        
    }
}
