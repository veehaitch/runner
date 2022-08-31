using GitHub.Runner.Worker;
using GitHub.Runner.Worker.Container;
using Xunit;
using Moq;
using GitHub.Runner.Worker.Container.ContainerHooks;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;


namespace GitHub.Runner.Common.Tests.Worker
{

    public sealed class ContainerOperationProviderL0
    {

        private TestHostContext _hc;
        private Mock<IExecutionContext> _ec;
        private Mock<IDockerCommandManager> _dockerManager;
        private Mock<IContainerHookManager> _containerHookManager;
        private ContainerOperationProvider containerOperationProvider;

        private Mock<IJobServerQueue> serverQueue;

        private Mock<IPagingLogger> pagingLogger;

        private List<string> healthyDockerStatus = new List<string> { "healthy" };
        private List<string> dockerLogs = new List<string> { "log1", "log2", "log3" };

        private ContainerInfo containerInfo;

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async void Healthchecktest_healthyDocker()
        {
            //Arrange
            Setup();
            _dockerManager.Setup(x => x.DockerInspect(_ec.Object, It.IsAny<string>(), It.IsAny<string>())).Returns(Task.FromResult(healthyDockerStatus));

            //Act
            var result = await containerOperationProvider.Healthcheck(_ec.Object, containerInfo);

            //Assert
            _dockerManager.Verify(dm => dm.DockerInspectLogs(It.IsAny<IExecutionContext>(), It.IsAny<string>()), Times.Never());

        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async void Healthchecktest_dockerError()
        {
            //Arrange
            Setup();
            _dockerManager.Setup(x => x.DockerInspectLogs(_ec.Object, containerInfo.ContainerId)).Returns(Task.FromResult(dockerLogs));

            //Act
            //Asert
            await Assert.ThrowsAsync<InvalidOperationException>(() => containerOperationProvider.ContainerHealthcheckLogs(_ec.Object, containerInfo, "error"));

        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async void Healthchecktest_dockerError_inspectLogs()
        {
            //Arrange
            Setup();
            _dockerManager.Setup(x => x.DockerInspectLogs(_ec.Object, containerInfo.ContainerId)).Returns(Task.FromResult(dockerLogs));

            try
            {
                //Act
                await containerOperationProvider.ContainerHealthcheckLogs(_ec.Object, containerInfo, "error");

            }
            catch (InvalidOperationException)
            {

                //Assert
                _ec.Verify(pL => pL.Write(It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(3));

            }

        }

        private void Setup([CallerMemberName] string testName = "")
        {
            containerInfo = new ContainerInfo() { ContainerImage = "ubuntu:16.04" };
            _hc = new TestHostContext(this, "name");
            _ec = new Mock<IExecutionContext>();
            serverQueue = new Mock<IJobServerQueue>();
            pagingLogger = new Mock<IPagingLogger>();

            _dockerManager = new Mock<IDockerCommandManager>();
            _containerHookManager = new Mock<IContainerHookManager>();
            containerOperationProvider = new ContainerOperationProvider();

            _hc.SetSingleton<IDockerCommandManager>(_dockerManager.Object);
            _hc.SetSingleton<IJobServerQueue>(serverQueue.Object);
            _hc.SetSingleton<IPagingLogger>(pagingLogger.Object);

            _hc.SetSingleton<IDockerCommandManager>(_dockerManager.Object);
            _hc.SetSingleton<IContainerHookManager>(_containerHookManager.Object);

            var list = new List<string>();
            list.Add("result");

            _ec.Setup(x => x.Global).Returns(new GlobalContext());

            containerOperationProvider.Initialize(_hc);
        }
    }
}