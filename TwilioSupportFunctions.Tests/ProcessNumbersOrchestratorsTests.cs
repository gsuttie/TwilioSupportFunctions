using System.Net.Http;
using System.Threading.Tasks;
using AutoFixture;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace TwilioSupportFunctions.Tests
{
    public class ProcessNumbersOrchestratorsTests
    {
        private readonly Fixture _fixture = new Fixture();

        //[Fact]
        //public async Task Run_Orchectrator_Client()
        //{
        //    var clientMock = new Mock<IDurableOrchestrationContext>(MockBehavior.Strict);
        //    var requestMock = new Mock<HttpRequestMessage>();
        //    var id = "8e503c5e-19de-40e1-932d-298c4263115b";
        //    clientMock.Setup(client => client.StartNewAsync("O_CallSupport", null)).Returns(Task.FromResult<string>(id));
        //    var request = requestMock.Object;

        //    clientMock.Setup(client => client.CreateCheckStatusResponse(request, id, true));

        //    var result = ProcessNumbersOrchestrators.CallSupport(clientMock.Object, GetFakeLogger());
        //    try
        //    {

        //        clientMock.Verify(client => client.StartNewAsync("DurableFunctions", null));
        //        clientMock.Verify(client => client.CreateCheckStatusResponse(request, id));
        //    }
        //    catch (MockException ex)
        //    {
        //        Assert.Fail();
        //    }
        //}


        [Fact]
        public void CallA_GetNumbersFromStorage_Returns_PhoneNumbers()
        {
            //Arrange
            var durableActivityContextMock = new Mock<IDurableActivityContext>(MockBehavior.Strict);
            durableActivityContextMock.Setup(x => x.GetInput<string>()).Returns("+44123456789");

            //Act
            var result = ProcessNumbersActivities.GetNumbersFromStorage("+44123456789", GetFakeLogger());

            //Assert
            Assert.NotNull(result);
        }


        private static ILogger GetFakeLogger()
        {
            return new Mock<ILogger>().Object;
        }
    }
}
