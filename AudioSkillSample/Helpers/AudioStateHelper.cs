using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using Amazon.Lambda.Tools;
using Amazon.Runtime;
using AudioSkillSample.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AudioSkillSample.Helpers
{
    public class AudioStateHelper
    {
        private string accessKey = "AKIAJOQ4PQKYSMBZSCAA";
        private string secretKey = "ZmCcTX83fut96qfpHmgn2CyGUDOYq4mttZnl0UpD";

        //private string accessKey = "[your access key for dynamoDB permissions goes here]";
        //private string secretKey = "[your secret key for dynamoDB permissions goes here]";

        private static string dynamoDBTableName = "AudioStates";
        private static string hashKey = "UserId";

        private AmazonDynamoDBClient client { get; set; }
        private DynamoDBContext context { get; set; }

        public AudioStateHelper()
        {
            var credentials = new BasicAWSCredentials(accessKey, secretKey);
            // We need the region to be USEast because (at least for now) Alexa skills are 
            //  hosted in Amazon's US East region
            client = new AmazonDynamoDBClient(credentials, RegionEndpoint.USEast1);
        }

        
        public async Task VerifyTable()
        {
            await this.VerifyTable(dynamoDBTableName);
        }

        public async Task VerifyTable(string tableName)
        {
            var tableResponse = await client.ListTablesAsync();
            if (!tableResponse.TableNames.Contains(tableName) &&
                !CreateTable(tableName).Result)
                throw new Exception("Could not find or create table: " + tableName);
            // Set the context
            context = new DynamoDBContext(client);
        }

        private async Task<bool> CreateTable(string tableName)
        {
            await client.CreateTableAsync(new CreateTableRequest
            {
                TableName = tableName,
                ProvisionedThroughput = new ProvisionedThroughput
                {
                    ReadCapacityUnits = 3,
                    WriteCapacityUnits = 1
                },
                KeySchema = new List<KeySchemaElement>
                    {
                        new KeySchemaElement
                        {
                            AttributeName = hashKey,
                            KeyType = KeyType.HASH
                        }
                    },
                AttributeDefinitions = new List<AttributeDefinition>
                    {
                        new AttributeDefinition { AttributeName = hashKey, AttributeType=ScalarAttributeType.S }
                    }
            });

            bool isTableAvailable = false;
            int waitLimit = 10;
            int waitCount = 0;
            while (!isTableAvailable)
            {
                Thread.Sleep(5000);
                var tableStatus = await client.DescribeTableAsync(tableName);
                isTableAvailable = tableStatus.Table.TableStatus == "ACTIVE";
                waitCount++;
                if (waitLimit == waitCount)
                    return false;
            }

            return true;
        }

        public async Task SaveAudioState(AudioState state)
        {
            await context.SaveAsync<AudioState>(state);
        }

        public async Task<AudioState> GetAudioState(string userId)
        {
            List<ScanCondition> conditions = new List<ScanCondition>();
            conditions.Add(new ScanCondition("UserId", ScanOperator.Equal, userId));
            var allDocs = await context.ScanAsync<AudioState>(conditions).GetRemainingAsync();
            if (allDocs != null && allDocs.Count != 0)
            {
                return allDocs.FirstOrDefault();
            }

            var initialState = new AudioState() { UserId = userId };
            initialState.State = AudioSkillSample.Assets.Constants.SetDefaultState();
            return initialState;

        }

    }
}


