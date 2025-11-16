// QueryForEachWithUserDataTests.cs

using NUnit.Framework;
using UnsafeEcs.Core.Entities;

namespace UnsafeEcs.Tests.Editor.EntityManagerTests.Query
{
    [TestFixture]
    public class QueryForEachWithUserDataTests : EntityQueryTest
    {
        private struct UserData
        {
            public int intValue;
            public float floatValue;
        }

        [Test]
        public void ForEach_WithUserData_PassesUserDataToAction()
        {
            var entity = CreateEntityWithComponents(typeof(ComponentA));

            var userData = new UserData { intValue = 42, floatValue = 3.14f };
            var query = CreateTestQuery().With<ComponentA>();

            var executed = false;
            query.ForEach(userData, (UserData data, ref Entity e) =>
            {
                executed = true;
                Assert.AreEqual(42, data.intValue);
                Assert.AreEqual(3.14f, data.floatValue);
            });

            Assert.IsTrue(executed);
        }

        [Test]
        public void ForEach_WithUserDataAndOneComponent_WorksCorrectly()
        {
            var entity = CreateEntityWithComponents(typeof(ComponentA));
            entityManager.SetComponent(entity, new ComponentA());

            var userData = new UserData { intValue = 100 };
            var query = CreateTestQuery().With<ComponentA>();

            var executed = false;
            query.ForEach(userData, (UserData data, ref Entity e, ref ComponentA _) =>
            {
                executed = true;
                Assert.AreEqual(100, data.intValue);
                Assert.AreEqual(entity, e);
            });

            Assert.IsTrue(executed);
        }

        [Test]
        public void ForEach_WithUserDataAndMultipleComponents_WorksCorrectly()
        {
            var entity = CreateEntityWithComponents(typeof(ComponentA), typeof(ComponentB));

            var userData = new UserData { intValue = 200, floatValue = 2.5f };
            var query = CreateTestQuery().With<ComponentA, ComponentB>();

            var executed = false;
            query.ForEach(userData, (UserData data, ref Entity e, ref ComponentA _, ref ComponentB _) =>
            {
                executed = true;
                Assert.AreEqual(200, data.intValue);
                Assert.AreEqual(2.5f, data.floatValue);
                Assert.AreEqual(entity, e);
            });

            Assert.IsTrue(executed);
        }

        [Test]
        public void ForEach_WithUserDataAndThreeComponents_WorksCorrectly()
        {
            var entity = CreateEntityWithComponents(typeof(ComponentA), typeof(ComponentB), typeof(ComponentC));

            var userData = new UserData { intValue = 300 };
            var query = CreateTestQuery().With<ComponentA, ComponentB, ComponentC>();

            var executed = false;
            query.ForEach(userData, (UserData data, ref Entity e, ref ComponentA _, ref ComponentB _, ref ComponentC _) =>
            {
                executed = true;
                Assert.AreEqual(300, data.intValue);
                Assert.AreEqual(entity, e);
            });

            Assert.IsTrue(executed);
        }

        [Test]
        public void ForEach_WithUserDataAndFourComponents_WorksCorrectly()
        {
            var entity = CreateEntityWithComponents(typeof(ComponentA), typeof(ComponentB), typeof(ComponentC), typeof(ComponentD));

            var userData = new UserData { intValue = 400 };
            var query = CreateTestQuery().With<ComponentA, ComponentB, ComponentC, ComponentD>();

            var executed = false;
            query.ForEach(userData, (UserData data, ref Entity e, ref ComponentA _, ref ComponentB _, ref ComponentC _, ref ComponentD _) =>
            {
                executed = true;
                Assert.AreEqual(400, data.intValue);
                Assert.AreEqual(entity, e);
            });

            Assert.IsTrue(executed);
        }

        [Test]
        public void ForEach_WithUserDataAndFiveComponents_WorksCorrectly()
        {
            var entity = CreateEntityWithComponents(
                typeof(ComponentA), typeof(ComponentB), typeof(ComponentC),
                typeof(ComponentD), typeof(ComponentE));

            var userData = new UserData { intValue = 500 };
            var query = CreateTestQuery()
                .With<ComponentA, ComponentB, ComponentC, ComponentD, ComponentE>();

            var executed = false;
            query.ForEach(userData, (UserData data, ref Entity e, ref ComponentA _, ref ComponentB _, ref ComponentC _, ref ComponentD _, ref ComponentE _) =>
            {
                executed = true;
                Assert.AreEqual(500, data.intValue);
                Assert.AreEqual(entity, e);
            });

            Assert.IsTrue(executed);
        }

        [Test]
        public void ForEach_WithUserData_AvoidsCapture()
        {
            // This test demonstrates the primary benefit: avoiding lambda captures
            var entity1 = CreateEntityWithComponents(typeof(ComponentA));
            var entity2 = CreateEntityWithComponents(typeof(ComponentA));

            var deltaTime = 0.016f; // Would normally be captured
            var query = CreateTestQuery().With<ComponentA>();

            var count = 0;
            query.ForEach(deltaTime, (float dt, ref Entity e, ref ComponentA _) =>
            {
                count++;
                // dt is passed as parameter, not captured
                Assert.Greater(dt, 0.0f);
            });

            Assert.AreEqual(2, count);
        }

        [Test]
        public void ForEach_WithUserDataMultipleEntities_ExecutesForEach()
        {
            var entity1 = CreateEntityWithComponents(typeof(ComponentA));
            var entity2 = CreateEntityWithComponents(typeof(ComponentA));
            var entity3 = CreateEntityWithComponents(typeof(ComponentA));

            var userData = new UserData { intValue = 1 };
            var query = CreateTestQuery().With<ComponentA>();

            var count = 0;
            query.ForEach(userData, (UserData data, ref Entity e, ref ComponentA _) =>
            {
                count += data.intValue;
            });

            Assert.AreEqual(3, count);
        }

        // ==================== TWO USER DATA PARAMETERS ====================

        [Test]
        public void ForEach_With2UserData_PassesBothToAction()
        {
            var entity = CreateEntityWithComponents(typeof(ComponentA));

            var deltaTime = 0.016f;
            var multiplier = 2.5f;
            var query = CreateTestQuery().With<ComponentA>();

            var executed = false;
            query.ForEach(deltaTime, multiplier, (float dt, float mult, ref Entity e, ref ComponentA _) =>
            {
                executed = true;
                Assert.AreEqual(0.016f, dt, 0.0001f);
                Assert.AreEqual(2.5f, mult, 0.0001f);
                Assert.AreEqual(entity, e);
            });

            Assert.IsTrue(executed);
        }

        [Test]
        public void ForEach_With2UserDataNoComponents_WorksCorrectly()
        {
            var entity = CreateEntityWithComponents(typeof(ComponentA));

            var value1 = 100;
            var value2 = "test";
            var query = CreateTestQuery().With<ComponentA>();

            var executed = false;
            query.ForEach(value1, value2, (int v1, string v2, ref Entity e) =>
            {
                executed = true;
                Assert.AreEqual(100, v1);
                Assert.AreEqual("test", v2);
            });

            Assert.IsTrue(executed);
        }

        [Test]
        public void ForEach_With2UserDataAnd2Components_WorksCorrectly()
        {
            var entity = CreateEntityWithComponents(typeof(ComponentA), typeof(ComponentB));

            var speed = 10.0f;
            var enabled = true;
            var query = CreateTestQuery().With<ComponentA, ComponentB>();

            var executed = false;
            query.ForEach(speed, enabled, (float spd, bool enb, ref Entity e, ref ComponentA _, ref ComponentB _) =>
            {
                executed = true;
                Assert.AreEqual(10.0f, spd);
                Assert.IsTrue(enb);
                Assert.AreEqual(entity, e);
            });

            Assert.IsTrue(executed);
        }

        [Test]
        public void ForEach_With2UserDataAnd3Components_WorksCorrectly()
        {
            var entity = CreateEntityWithComponents(typeof(ComponentA), typeof(ComponentB), typeof(ComponentC));

            var gravity = -9.81f;
            var timestep = 0.02f;
            var query = CreateTestQuery().With<ComponentA, ComponentB, ComponentC>();

            var executed = false;
            query.ForEach(gravity, timestep, (float g, float ts, ref Entity e, ref ComponentA _, ref ComponentB _, ref ComponentC _) =>
            {
                executed = true;
                Assert.AreEqual(-9.81f, g, 0.0001f);
                Assert.AreEqual(0.02f, ts, 0.0001f);
            });

            Assert.IsTrue(executed);
        }

        // ==================== THREE USER DATA PARAMETERS ====================

        [Test]
        public void ForEach_With3UserData_PassesAllToAction()
        {
            var entity = CreateEntityWithComponents(typeof(ComponentA));

            var deltaTime = 0.016f;
            var gravity = -9.81f;
            var damping = 0.99f;
            var query = CreateTestQuery().With<ComponentA>();

            var executed = false;
            query.ForEach(deltaTime, gravity, damping, (float dt, float g, float d, ref Entity e, ref ComponentA _) =>
            {
                executed = true;
                Assert.AreEqual(0.016f, dt, 0.0001f);
                Assert.AreEqual(-9.81f, g, 0.0001f);
                Assert.AreEqual(0.99f, d, 0.0001f);
                Assert.AreEqual(entity, e);
            });

            Assert.IsTrue(executed);
        }

        [Test]
        public void ForEach_With3UserDataNoComponents_WorksCorrectly()
        {
            var entity = CreateEntityWithComponents(typeof(ComponentA));

            var value1 = 100;
            var value2 = 3.14f;
            var value3 = "test";
            var query = CreateTestQuery().With<ComponentA>();

            var executed = false;
            query.ForEach(value1, value2, value3, (int v1, float v2, string v3, ref Entity e) =>
            {
                executed = true;
                Assert.AreEqual(100, v1);
                Assert.AreEqual(3.14f, v2, 0.0001f);
                Assert.AreEqual("test", v3);
            });

            Assert.IsTrue(executed);
        }

        [Test]
        public void ForEach_With3UserDataAnd2Components_WorksCorrectly()
        {
            var entity = CreateEntityWithComponents(typeof(ComponentA), typeof(ComponentB));

            var speed = 10.0f;
            var direction = 1;
            var enabled = true;
            var query = CreateTestQuery().With<ComponentA, ComponentB>();

            var executed = false;
            query.ForEach(speed, direction, enabled, (float spd, int dir, bool enb, ref Entity e, ref ComponentA _, ref ComponentB _) =>
            {
                executed = true;
                Assert.AreEqual(10.0f, spd);
                Assert.AreEqual(1, dir);
                Assert.IsTrue(enb);
                Assert.AreEqual(entity, e);
            });

            Assert.IsTrue(executed);
        }

        [Test]
        public void ForEach_With3UserDataMultipleEntities_WorksCorrectly()
        {
            var entity1 = CreateEntityWithComponents(typeof(ComponentA));
            var entity2 = CreateEntityWithComponents(typeof(ComponentA));
            var entity3 = CreateEntityWithComponents(typeof(ComponentA));

            var add = 1;
            var multiply = 2;
            var offset = 10;
            var query = CreateTestQuery().With<ComponentA>();

            var sum = 0;
            query.ForEach(add, multiply, offset, (int a, int m, int o, ref Entity e, ref ComponentA _) =>
            {
                sum += (a * m) + o;
            });

            Assert.AreEqual(36, sum); // 3 entities × ((1 * 2) + 10) = 3 × 12 = 36
        }
    }
}
