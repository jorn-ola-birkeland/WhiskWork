﻿using System;
using System.Collections.Specialized;
using System.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WhiskWork.Core;
using System.IO;
using WhiskWork.Web;
using WhiskWork.UnitTest.Properties;

namespace WhiskWork.UnitTest
{
    [TestClass]
    public class HtmlRenderTest
    {
        private TestWorkflowRepository _workflowRepository;
        private TestWorkItemRepository _workItemRepository;
        private Workflow _wp;

        [TestInitialize]
        public void Init()
        {
            _workflowRepository = new TestWorkflowRepository();
            _workItemRepository = new TestWorkItemRepository();
            _wp = new Workflow(_workflowRepository, _workItemRepository);

        }
            

        [TestMethod]
        public void ShouldRenderSingleStepWorkflow()
        {
            _workflowRepository.Add("/analysis", "/", 1, WorkStepType.Begin, "cr");

            var doc = GetFullDocument();

            Assert.IsNotNull(doc.SelectSingleNode("/html/body/ol/li[@id=\"analysis\"]"));
        }

        [TestMethod]
        public void ShouldRenderSingleStepWithTitleWorkflow()
        {
            _workflowRepository.Add("/analysis", "/", 1, WorkStepType.Begin, "cr", "Analysis");

            var doc = GetFullDocument();

            Assert.AreEqual("Analysis", doc.SelectSingleNode("/html/body/ol/li[@id=\"analysis\"]/h1").InnerText.Trim());
        }


        [TestMethod]
        public void ShouldRenderStepsInRightOrder()
        {
            _workflowRepository.Add("/test", "/", 3, WorkStepType.End, "cr", "Test");
            _workflowRepository.Add("/analysis", "/", 1, WorkStepType.Begin, "cr", "Analysis");
            _workflowRepository.Add("/development", "/", 2, WorkStepType.Normal, "cr", "Development");

            var doc = GetFullDocument();

            Assert.AreEqual("analysis",doc.SelectSingleNode("/html/body/ol/li[position()=1]/@id").Value);
            Assert.AreEqual("development", doc.SelectSingleNode("/html/body/ol/li[position()=2]/@id").Value);
            Assert.AreEqual("test", doc.SelectSingleNode("/html/body/ol/li[position()=3]/@id").Value);


        }

        [TestMethod]
        public void ShouldRenderSubSteps()
        {
            _workflowRepository.Add("/development", "/", 2, WorkStepType.Normal, "cr", "Development ");
            _workflowRepository.Add("/development/inprocess", "/development", 1, WorkStepType.Normal, "cr", "In process");
            _workflowRepository.Add("/development/done", "/development", 2, WorkStepType.Normal, "cr", "Dev. done");

            var doc = GetFullDocument();

            Assert.IsNotNull(doc.SelectSingleNode("/html/body/ol/li[@id=\"development\"]/ol/li[@id=\"development.inprocess\"]"));
            Assert.IsNotNull(doc.SelectSingleNode("/html/body/ol/li[@id=\"development\"]/ol/li[@id=\"development.done\"]"));
        }

        [TestMethod]
        public void ShouldRenderClasses()
        {
            _workflowRepository.Add("/development", "/", 2, WorkStepType.Normal, "cr", "Development ");
            _workflowRepository.Add("/development/inprocess", "/development", 1, WorkStepType.Normal, "cr", "In process");
            _workflowRepository.Add("/development/done", "/development", 2, WorkStepType.Normal, "cr", "Dev. done");

            var doc = GetFullDocument();

            Assert.AreEqual("workstep step-cr inprocess", doc.SelectSingleNode("//li[@id=\"development.inprocess\"]/@class").Value);
        }

        [TestMethod]
        public void ShouldRenderSingleWorkItemInSingleStepWorkflow()
        {
            _workflowRepository.Add("/analysis", "/", 1, WorkStepType.Begin, "cr", "Analysis");
            _wp.CreateWorkItem("cr1","/analysis");

            var doc = GetFullDocument();

            Assert.IsNotNull(doc.SelectSingleNode("/html/body/ol/li[@id=\"analysis\"]/ol/li[@id=\"cr1\"]"));
        }

        [TestMethod]
        public void ShouldRenderTwoWorkItemsInSingleStepWorkflow()
        {
            _workflowRepository.Add("/analysis", "/", 1, WorkStepType.Begin, "cr", "Analysis");
            _wp.CreateWorkItem("cr1", "/analysis");
            _wp.CreateWorkItem("cr2", "/analysis");

            var doc = GetFullDocument();

            Assert.AreEqual("cr1", doc.SelectSingleNode("/html/body/ol/li[@id=\"analysis\"]/ol/li[position()=1]/@id").Value);
            Assert.AreEqual("cr2", doc.SelectSingleNode("/html/body/ol/li[@id=\"analysis\"]/ol/li[position()=2]/@id").Value);
        }

        [TestMethod]
        public void ShouldRenderAWorkItemInEachStepForAThreeStepWorkflow()
        {
            _workflowRepository.Add("/analysis", "/", 1, WorkStepType.Begin, "cr");
            _workflowRepository.Add("/development", "/", 2, WorkStepType.Normal, "cr");
            _workflowRepository.Add("/done", "/", 3, WorkStepType.End, "cr");
            _wp.CreateWorkItem("cr1", "/analysis");
            _wp.CreateWorkItem("cr2", "/analysis");
            _wp.CreateWorkItem("cr3", "/analysis");

            _wp.UpdateWorkItem("cr2","/development", new NameValueCollection());
            _wp.UpdateWorkItem("cr3", "/done", new NameValueCollection());

            var doc = GetFullDocument();

            Assert.AreEqual("cr1", doc.SelectSingleNode("/html/body/ol/li[@id=\"analysis\"]/ol/li/@id").Value);
            Assert.AreEqual("cr2", doc.SelectSingleNode("/html/body/ol/li[@id=\"development\"]/ol/li/@id").Value);
            Assert.AreEqual("cr3", doc.SelectSingleNode("/html/body/ol/li[@id=\"done\"]/ol/li/@id").Value);
        }

        [TestMethod]
        public void ShouldRenderParallelStepAsUnorderedList()
        {
            _workflowRepository.Add("/feedback", "/", 1, WorkStepType.Parallel, "cr");
            _workflowRepository.Add("/feedback/review", "/feedback", 1, WorkStepType.Normal, "cr");
            _workflowRepository.Add("/feedback/test", "/feedback", 2, WorkStepType.Normal, "cr-test");

            var doc = GetFullDocument();

            Assert.AreEqual("feedback.review", doc.SelectSingleNode("/html/body/ol/li[@id=\"feedback\"]/ul/li[position()=1]/@id").Value);
            Assert.AreEqual("feedback.test", doc.SelectSingleNode("/html/body/ol/li[@id=\"feedback\"]/ul/li[position()=2]/@id").Value);
        }


        [TestMethod]
        public void ShouldNotRenderEmptyListTagInLeafStepWithNoWorkItems()
        {
            _workflowRepository.Add("/development", "/", 1, WorkStepType.Begin, "cr");

            var doc = GetFullDocument();

            Assert.IsNull(doc.SelectSingleNode("//li[@id=\"development\"]/ol"));
            Assert.IsNull(doc.SelectSingleNode("//li[@id=\"development\"]/ul"));
        }

        [TestMethod]
        public void ShouldNotRenderH1TagIfWorkStepTitleIsEmpty()
        {
            _workflowRepository.Add("/development", "/", 1, WorkStepType.Begin, "cr");

            var doc = GetFullDocument();

            Assert.IsNull(doc.SelectSingleNode("//li[@id=\"development\"]/h1"));
        }


        [TestMethod]
        public void ShouldRenderWorkItemWithRightClasses()
        {
            _workflowRepository.Add("/development", "/", 1, WorkStepType.Begin, "cr");
            _wp.CreateWorkItem("cr1", "/development");

            var doc = GetFullDocument();

            var classAttribute = doc.SelectSingleNode("//li[@id=\"development\"]/ol/li[@id=\"cr1\"]/@class");
            Assert.IsNotNull(classAttribute);
            Assert.AreEqual("workitem cr", classAttribute.Value);
        }


        [TestMethod]
        public void ShouldRenderParallelStepsWithRightClass()
        {
            _workflowRepository.Add("/feedback", "/", 1, WorkStepType.Parallel, "cr");
            _workflowRepository.Add("/feedback/review", "/feedback", 1, WorkStepType.Normal, "cr-review");

            var doc = GetFullDocument();

            Assert.AreEqual("workstep step-cr-review review", doc.SelectSingleNode("/html/body/ol/li[@id=\"feedback\"]/ul/li[position()=1]/@class").Value);
        }

        [TestMethod]
        public void ShouldRenderParalleledChildItemsWithRightClasses()
        {
            _workflowRepository.Add("/development", "/", 1, WorkStepType.Begin, "cr");
            _workflowRepository.Add("/feedback", "/", 2, WorkStepType.Parallel, "cr");
            _workflowRepository.Add("/feedback/review", "/feedback", 1, WorkStepType.Normal, "cr-review");
            _workflowRepository.Add("/feedback/test", "/feedback", 2, WorkStepType.Normal, "cr-test");

            _wp.CreateWorkItem("cr1", "/development");
            _wp.UpdateWorkItem("cr1", "/feedback", new NameValueCollection());
            
            var doc = GetFullDocument();

            Assert.AreEqual("workitem cr cr-test", doc.SelectSingleNode("//li[@id=\"development\"]/ol/li[@id=\"cr1.test\"]/@class").Value);
            Assert.AreEqual("workitem cr cr-review", doc.SelectSingleNode("//li[@id=\"feedback.review\"]/ol/li[@id=\"cr1.review\"]/@class").Value);
        }

        [TestMethod]
        public void ShouldNotRenderParallelLockedWorkItem()
        {
            _workflowRepository.Add("/development", "/", 1, WorkStepType.Begin, "cr");
            _workflowRepository.Add("/feedback", "/", 2, WorkStepType.Parallel, "cr");
            _workflowRepository.Add("/feedback/review", "/feedback", 1, WorkStepType.Normal, "cr-review");
            _workflowRepository.Add("/feedback/test", "/feedback", 2, WorkStepType.Normal, "cr-test");

            _wp.CreateWorkItem("cr1", "/development");
            _wp.UpdateWorkItem("cr1", "/feedback", new NameValueCollection());

            var doc = GetFullDocument();

            Assert.IsNull(doc.SelectSingleNode("//li[@id=\"feedback\"]/ol/li[@id=\"cr1\"]"));
        }


        [TestMethod]
        public void ShouldRenderCorrectOuterExpandStepClass()
        {
            _workflowRepository.Add("/analysis", "/", 1, WorkStepType.Begin, "cr");
            _workflowRepository.Add("/development", "/", 2, WorkStepType.Begin, "cr");
            _workflowRepository.Add("/development/inprocess", "/development", 1, WorkStepType.Expand, "cr");

            var doc = GetFullDocument();

            Assert.IsNotNull(doc.SelectSingleNode("/html/body/ol/li[@id=\"development\"]/ol/li[@id=\"development.inprocess\"]"));
            Assert.AreEqual("inprocess",doc.SelectSingleNode("//li[@id=\"development.inprocess\"]/@class").Value);
        }

        [TestMethod]
        public void ShouldRenderCorrectClassForExpandTemplate()
        {
            _workflowRepository.Add("/analysis", "/", 1, WorkStepType.Begin, "cr");
            _workflowRepository.Add("/development", "/", 2, WorkStepType.Begin, "cr");
            _workflowRepository.Add("/development/inprocess", "/development", 1, WorkStepType.Expand, "cr");

            var doc = GetFullDocument();

            Assert.IsNotNull(doc.SelectSingleNode("/html/body/ol/li[@id=\"development\"]/ol/li[@id=\"development.inprocess\"]"));
            Assert.AreEqual("expand", doc.SelectSingleNode("//li[@id=\"development.inprocess\"]/ol/li/@class").Value);
        }


        [TestMethod]
        public void ShouldRenderWorkStepForWorkItemInExpandTemplate()
        {
            _workflowRepository.Add("/analysis", "/", 1, WorkStepType.Begin, "cr");
            _workflowRepository.Add("/development", "/", 2, WorkStepType.Begin, "cr");
            _workflowRepository.Add("/development/inprocess", "/development", 1, WorkStepType.Expand, "cr");

            var doc = GetFullDocument();

            Assert.AreEqual("workstep step-cr", doc.SelectSingleNode("//li[@id=\"development.inprocess\"]/ol/li[@class='expand']/ol/li[position()=1]/@class").Value);
        }

        [TestMethod]
        public void ShouldRenderFirstDescendantStepAfterWorkItemStepInExpandTemplate()
        {
            _workflowRepository.Add("/analysis", "/", 1, WorkStepType.Begin, "cr");
            _workflowRepository.Add("/development", "/", 2, WorkStepType.Begin, "cr");
            _workflowRepository.Add("/development/inprocess", "/development", 1, WorkStepType.Expand, "cr");
            _workflowRepository.Add("/development/inprocess/tasks", "/development/inprocess", 1, WorkStepType.Normal, "task");

            var doc = GetFullDocument();

            Assert.IsNotNull(doc.SelectSingleNode("//li[@id=\"development.inprocess\"]/ol/li[@class='expand']/ol/li[position()=2]"));
            Assert.AreEqual("development.inprocess.tasks", doc.SelectSingleNode("//li[@id=\"development.inprocess\"]/ol/li[@class='expand']/ol/li[position()=2]/@id").Value);
            Assert.AreEqual("workstep step-task tasks", doc.SelectSingleNode("//li[@id=\"development.inprocess\"]/ol/li[@class='expand']/ol/li[position()=2]/@class").Value);
        }

        [TestMethod]
        public void ShouldRenderMultiLevelDescendantsStepAfterWorkItemStepInExpandTemplate()
        {
            _workflowRepository.Add("/analysis", "/", 1, WorkStepType.Begin, "cr");
            _workflowRepository.Add("/development", "/", 2, WorkStepType.Begin, "cr");
            _workflowRepository.Add("/development/inprocess", "/development", 1, WorkStepType.Expand, "cr");
            _workflowRepository.Add("/development/inprocess/tasks", "/development/inprocess", 1, WorkStepType.Normal, "task");
            _workflowRepository.Add("/development/inprocess/tasks/new", "/development/inprocess/tasks", 1, WorkStepType.Begin, "task");

            var doc = GetFullDocument();

            Assert.AreEqual("development.inprocess.tasks.new", doc.SelectSingleNode("//li[@class='expand']/ol/li[@id='development.inprocess.tasks']/ol/li/@id").Value);
        }

        [TestMethod]
        public void ShouldRenderTransientStepBeforeExpandStep()
        {
            _workflowRepository.Add("/analysis", "/", 1, WorkStepType.Begin, "cr");
            _workflowRepository.Add("/development", "/", 2, WorkStepType.Begin, "cr");
            _workflowRepository.Add("/development/inprocess", "/development", 1, WorkStepType.Expand, "cr");

            Create("/analysis", "cr1");
            Move("/development", "cr1");


            var doc = GetFullDocument();

            Assert.AreEqual("transient", doc.SelectSingleNode("//li[@id=\"development.inprocess\"]/ol/li[position()=1]/@class").Value);
            Assert.AreEqual("expand", doc.SelectSingleNode("//li[@id=\"development.inprocess\"]/ol/li[position()=2]/@class").Value);
            Assert.IsNull(doc.SelectSingleNode("//li[@id=\"development.inprocess\"]/ol/li[position()=3]/@class"));
        }

        [TestMethod]
        public void ShouldRenderTransientStepWithCorrectId()
        {
            _workflowRepository.Add("/analysis", "/", 1, WorkStepType.Begin, "cr");
            _workflowRepository.Add("/development", "/", 2, WorkStepType.Begin, "cr");
            _workflowRepository.Add("/development/inprocess", "/development", 1, WorkStepType.Expand, "cr");

            Create("/analysis", "cr1");
            Move("/development", "cr1");


            var doc = GetFullDocument();

            Assert.AreEqual("development.inprocess.cr1", doc.SelectSingleNode("//li[@id=\"development.inprocess\"]/ol/li[@class='transient']/@id").Value);
        }

        [TestMethod]
        public void ShouldRenderTitleForChildOfTransientStep()
        {
            _workflowRepository.Add("/analysis", "/", 1, WorkStepType.Begin, "cr");
            _workflowRepository.Add("/development", "/", 2, WorkStepType.Begin, "cr");
            _workflowRepository.Add("/development/inprocess", "/development", 1, WorkStepType.Expand, "cr");
            _workflowRepository.Add("/development/inprocess/tasks", "/development/inprocess", 1, WorkStepType.Normal, "task", "Tasks");
            _workflowRepository.Add("/development/inprocess/tasks/new", "/development/inprocess/tasks", 1, WorkStepType.Begin, "task");

            Create("/analysis", "cr1");
            Move("/development", "cr1");


            var doc = GetFullDocument();

            Assert.IsNotNull(doc.SelectSingleNode("//li[@class='transient']/ol/li[@class='tasks']/h1"));
        }

        [TestMethod]
        public void ExpandIntegrationTest1()
        {
            _workflowRepository.Add("/analysis", "/", 1, WorkStepType.Begin, "cr");
            _workflowRepository.Add("/development", "/", 2, WorkStepType.Begin, "cr");
            _workflowRepository.Add("/development/inprocess", "/development", 1, WorkStepType.Expand, "cr");
            _workflowRepository.Add("/development/inprocess/tasks", "/development/inprocess", 1, WorkStepType.Normal, "task");
            _workflowRepository.Add("/development/inprocess/tasks/new", "/development/inprocess/tasks", 1, WorkStepType.Begin, "task");
            _workflowRepository.Add("/development/inprocess/tasks/inprogress", "/development/inprocess/tasks", 1, WorkStepType.Normal, "task");
            _workflowRepository.Add("/development/inprocess/tasks/done", "/development/inprocess/tasks", 1, WorkStepType.End, "task");
            _workflowRepository.Add("/development/done", "/development", 2, WorkStepType.End, "cr");
            _workflowRepository.Add("/test", "/", 2, WorkStepType.Normal, "cr");
            _workflowRepository.Add("/done", "/", 2, WorkStepType.End, "cr");

            Create("/analysis", "cr1");
            Move("/development", "cr1");

            AssertIsAsExpected(Resources.ExpandIntegarationTest1);
        }

        [TestMethod]
        public void ExpandIntegrationTest2()
        {
            _workflowRepository.Add("/analysis", "/", 1, WorkStepType.Begin, "cr");
            _workflowRepository.Add("/development", "/", 2, WorkStepType.Begin, "cr");
            _workflowRepository.Add("/development/inprocess", "/development", 1, WorkStepType.Expand, "cr");
            _workflowRepository.Add("/development/inprocess/tasks", "/development/inprocess", 1, WorkStepType.Normal, "task");
            _workflowRepository.Add("/development/inprocess/tasks/new", "/development/inprocess/tasks", 1, WorkStepType.Begin, "task");
            _workflowRepository.Add("/development/inprocess/tasks/inprogress", "/development/inprocess/tasks", 1, WorkStepType.Normal, "task");
            _workflowRepository.Add("/development/inprocess/tasks/done", "/development/inprocess/tasks", 1, WorkStepType.End, "task");
            _workflowRepository.Add("/development/done", "/development", 2, WorkStepType.End, "cr");
            _workflowRepository.Add("/test", "/", 2, WorkStepType.Normal, "cr");
            _workflowRepository.Add("/done", "/", 2, WorkStepType.End, "cr");


            Create("/analysis", "cr1", "cr2");
            Move("/development", "cr1", "cr2");

            AssertIsAsExpected(Resources.ExpandIntegarationTest2);
        }


        [TestMethod]
        public void FullFeatureTest()
        {
            _workflowRepository.Add("/analysis", "/", 1, WorkStepType.Normal, "cr", "Analysis");
            _workflowRepository.Add("/analysis/inprocess", "/analysis", 1, WorkStepType.Begin, "cr");
            _workflowRepository.Add("/analysis/done", "/analysis", 1, WorkStepType.Normal, "cr");
            _workflowRepository.Add("/development", "/", 2, WorkStepType.Begin, "cr", "Development");
            _workflowRepository.Add("/development/inprocess", "/development", 1, WorkStepType.Expand, "cr");
            _workflowRepository.Add("/development/inprocess/tasks", "/development/inprocess", 1, WorkStepType.Normal, "task", "Tasks");
            _workflowRepository.Add("/development/inprocess/tasks/new", "/development/inprocess/tasks", 1, WorkStepType.Begin, "task");
            _workflowRepository.Add("/development/inprocess/tasks/inprocess", "/development/inprocess/tasks", 1, WorkStepType.Normal, "task");
            _workflowRepository.Add("/development/inprocess/tasks/done", "/development/inprocess/tasks", 1, WorkStepType.End, "task");
            _workflowRepository.Add("/development/done", "/development", 2, WorkStepType.End, "cr");
            _workflowRepository.Add("/feedback", "/", 3, WorkStepType.Parallel, "cr");
            _workflowRepository.Add("/feedback/review", "/feedback", 1, WorkStepType.Normal, "cr-review", "Review");
            _workflowRepository.Add("/feedback/demo", "/feedback", 2, WorkStepType.Normal, "cr-demo", "Demo");
            _workflowRepository.Add("/feedback/test", "/feedback", 3, WorkStepType.Normal, "cr-test", "Test");
            _workflowRepository.Add("/done", "/", 4, WorkStepType.End, "cr", "Done");

            Create("/analysis","cr1","cr2","cr3","cr4","cr5","cr6","cr7","cr8","cr9","cr10", "cr11", "cr12");
            Move("/analysis/done","cr4");
            Move("/development/inprocess", "cr5","cr6");
            Create("/development/inprocess/cr5", "cr5-1", "cr5-2", "cr5-3", "cr5-4");
            Create("/development/inprocess/cr6", "cr6-1", "cr6-2", "cr6-3", "cr6-4");
            Move("/development/inprocess/cr5/tasks/done", "cr5-1", "cr5-2");
            Move("/development/inprocess/cr6/tasks/inprocess", "cr6-1");
            Move("/development/done", "cr7", "cr8", "cr9","cr10", "cr11", "cr12");
            Move("/feedback/review", "cr7", "cr8");
            Move("/feedback/demo", "cr9", "cr10");
            Move("/feedback/test", "cr11");
            Move("/done", "cr7.review");
            Move("/done", "cr9.demo");
            Move("/done", "cr12");

            AssertIsAsExpected(Resources.FullIntegrationTest1);
        }

        private void Create(string path, params string[] workItemIds)
        {
            Array.ForEach(workItemIds, workItemId => _wp.CreateWorkItem(workItemId,path) );
        }

        private void Move(string path, params string[] workItemIds)
        {
            Array.ForEach(workItemIds, workItemId => _wp.UpdateWorkItem(workItemId, path, new NameValueCollection()));
        }


        private void AssertIsAsExpected(string expectedXml)
        {
            var expected = new XmlDocument();
            expected.LoadXml(expectedXml);

            var actual = GetFullDocument();
            Assert.AreEqual(expected.InnerXml, actual.InnerXml);
        }

        private XmlDocument GetFullDocument()
        {
            var htmlRenderer = new HtmlRenderer(_workflowRepository, _workItemRepository);

            var doc = new XmlDocument();
            using (var writeStream = new MemoryStream())
            {
                
                htmlRenderer.RenderFull(writeStream);

                var readStream = new MemoryStream(writeStream.ToArray());
                doc.Load(readStream);
            }
            return doc;
        }
    }
}
