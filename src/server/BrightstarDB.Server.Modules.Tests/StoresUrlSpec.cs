﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BrightstarDB.Client;
using BrightstarDB.Server.Modules.Model;
using BrightstarDB.Storage;
using NUnit.Framework;
using Nancy;
using Nancy.Responses.Negotiation;
using Nancy.Testing;
using Nancy.Testing.Fakes;
using Moq;

namespace BrightstarDB.Server.Modules.Tests
{
    [TestFixture]
    public class StoresUrlSpec
    {
        [TestFixtureSetUp]
        public void SetUp()
        {
            FakeRootPathProvider.RootPath = Path.GetDirectoryName(typeof(StoresUrlSpec).Assembly.Location);
        }

        [Test]
        public void TestGetReturnsOk()
        {
            var mockBrightstar = new Mock<IBrightstarService>();
            mockBrightstar.Setup(s => s.ListStores()).Returns(new string[0]);
            var app = new Browser(new FakeNancyBootstrapper(mockBrightstar.Object));
            var response = app.Get("/", c=>c.Accept(MediaRange.FromString("application/json")));
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }

        [Test]
        public void TestGetReturnsJsonArray()
        {
            // Setup
            var mockBrightstar = new Mock<IBrightstarService>();
            mockBrightstar.Setup(s => s.ListStores()).Returns(new string[] {"store1", "store2", "store3"});
            var app = new Browser(new FakeNancyBootstrapper(mockBrightstar.Object));

            // Execute
            var response = app.Get("/", c => c.Accept(MediaRange.FromString("application/json")));

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.ContentType, Contains.Substring("application/json"));
            Assert.That(response.Body, Is.Not.Null);
            var responseContent = response.Body.DeserializeJson <List<StoreResponseObject>>();
            Assert.That(responseContent, Is.Not.Null);
            Assert.That(responseContent.Count, Is.EqualTo(3));
            Assert.That(responseContent.Any(s=>s.Name.Equals("store1") && s.Jobs.Equals("store1/jobs")));
            Assert.That(responseContent.Any(s => s.Name.Equals("store2") && s.Jobs.Equals("store2/jobs")));
            Assert.That(responseContent.Any(s => s.Name.Equals("store3") && s.Jobs.Equals("store3/jobs")));
        }

        [Test]
        public void TestPostToCreateStore()
        {
            // Setup
            var mockBrightstar = new Mock<IBrightstarService>();
            mockBrightstar.Setup(s => s.DoesStoreExist("foo")).Returns(false);
            mockBrightstar.Setup(s => s.CreateStore("foo", PersistenceType.AppendOnly)).Verifiable("Expected CreateStore to be called");
            var app = new Browser(new FakeNancyBootstrapper(mockBrightstar.Object));

            // Execute
            var response = app.Post("/", with =>
                {
                    with.Accept(MediaRange.FromString("application/json"));
                    with.JsonBody(new CreateStoreRequestObject("foo", PersistenceType.AppendOnly));
                    with.AjaxRequest();
                });

            // Assert
            mockBrightstar.Verify();
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.ContentType, Contains.Substring("application/json"));
            Assert.That(response.Body, Is.Not.Null);
            var responseContent = response.Body.DeserializeJson<StoreResponseObject>();
            Assert.That(responseContent, Is.Not.Null);
            Assert.That(responseContent, Has.Property("Name").EqualTo("foo"));
            Assert.That(responseContent, Has.Property("Jobs").EqualTo("foo/jobs"));
        }

        [Test]
        public void TestCannotDuplicateStoreName()
        {
            // Setup
            var mockBrightstar = new Mock<IBrightstarService>();
            mockBrightstar.Setup(s => s.DoesStoreExist("foo")).Returns(true);
            var app = new Browser(new FakeNancyBootstrapper(mockBrightstar.Object));
            
            // Execute
            var response = app.Post("/", with =>
            {
                with.Accept(MediaRange.FromString("application/json"));
                with.JsonBody(new CreateStoreRequestObject("foo", PersistenceType.AppendOnly));
                with.AjaxRequest();
            });

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
        }

        [Test]
        public void TestCannotProvideNullStoreName()
        {
            // Setup
            var mockBrightstar = new Mock<IBrightstarService>();
            var app = new Browser(new FakeNancyBootstrapper(mockBrightstar.Object));

            // Execute
            var response = app.Post("/", with =>
            {
                with.Accept(MediaRange.FromString("application/json"));
                with.JsonBody(new CreateStoreRequestObject());
                with.AjaxRequest();
            });

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Test]
        public void TestInvalidStoreNameGivesBadRequest()
        {
            // Setup
            var mockBrightstar = new Mock<IBrightstarService>();
            mockBrightstar.Setup(s => s.CreateStore("/invalid/store_name")).Throws<ArgumentException>();
            var app = new Browser(new FakeNancyBootstrapper(mockBrightstar.Object));

            // Execute
            var response = app.Post("/", with =>
            {
                with.Accept(MediaRange.FromString("application/json"));
                with.JsonBody(new CreateStoreRequestObject("/invalid/store_name"));
                with.AjaxRequest();
            });

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Test]
        public void TestPersistenceTypeIsOptional()
        {
            // Setup
            var mockBrightstar = new Mock<IBrightstarService>();
            mockBrightstar.Setup(s => s.DoesStoreExist("foo")).Returns(false);
            mockBrightstar.Setup(s => s.CreateStore("foo")).Verifiable();
            var app = new Browser(new FakeNancyBootstrapper(mockBrightstar.Object));

            // Execute
            var response = app.Post("/", with =>
            {
                with.Accept(MediaRange.FromString("application/json"));
                with.JsonBody(new CreateStoreRequestObject("foo"));
                with.AjaxRequest();
            });

            mockBrightstar.Verify();
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.ContentType, Contains.Substring("application/json"));
            Assert.That(response.Body, Is.Not.Null);
            var responseContent = response.Body.DeserializeJson<StoreResponseObject>();
            Assert.That(responseContent, Is.Not.Null);
            Assert.That(responseContent, Has.Property("Name").EqualTo("foo"));
            Assert.That(responseContent, Has.Property("Jobs").EqualTo("foo/jobs"));            
        }
    }
}
