using KVD.Utils.DataStructures;
using NUnit.Framework;
using Unity.Collections;

namespace KVD.Utils.Editor.Tests.DataStructures
{
	public class UnsafePriorityListTests
	{
		[Test]
		public void AddTest()
		{
			// Arrange
			var list = new UnsafePriorityList<int, uint>(10, Allocator.Temp);
			using var autoDispose = list.AutoDispose();

			// Act
			list.Add(5, 2u);
			list.Add(10, 1u);
			list.Add(15, 3u);

			// Assert
			Assert.AreEqual(3u, list.Length);
			Assert.AreEqual(10, list.items[0]);
			Assert.AreEqual(5, list.items[1]);
			Assert.AreEqual(15, list.items[2]);
		}

		[Test]
		public void StaticRemoveTest()
		{
			// Arrange
			var list = new UnsafePriorityList<int, uint>(10, Allocator.Temp);
			using var autoDispose = list.AutoDispose();

			list.Add(5, 2u);
			list.Add(10, 1u);
			list.Add(15, 3u);

			// Act
			var removed = list.Remove(10);

			// Assert
			Assert.IsTrue(removed);
			Assert.AreEqual(2u, list.Length);
			Assert.AreEqual(5, list.items[0]);
			Assert.AreEqual(15, list.items[1]);
		}

		[Test]
		public void ResizeAdd()
		{
			// Arrange
			var list = new UnsafePriorityList<int, uint>(2, Allocator.Temp);
			using var autoDispose = list.AutoDispose();

			list.Add(5, 2u);
			list.Add(10, 1u);

			// Act
			list.Add(15, 3u); // This should trigger a resize

			// Assert
			Assert.AreEqual(3u, list.Length);
			Assert.AreEqual(10, list.items[0]);
			Assert.AreEqual(5, list.items[1]);
			Assert.AreEqual(15, list.items[2]);
		}

		[Test]
		public void MassiveAddTest()
		{
			// Arrange
			var list = new UnsafePriorityList<short, int>(1000, Allocator.Temp);
			using var autoDispose = list.AutoDispose();

			// Act
			for (short i = 0; i < 1000; i++)
			{
				var priority = i * (i % 2 == 0 ? 1 : -1); // Alternating priorities
				list.Add(i, priority);
			}

			// Assert
			Assert.AreEqual(1000u, list.Length);

			Assert.AreEqual(999, list.items[0]);
			Assert.AreEqual(997, list.items[1]);

			Assert.AreEqual(998, list.items[list.Length-1]);
			Assert.AreEqual(996, list.items[list.Length-2]);

			Assert.AreEqual(0, list.items[500]);
		}
	}
}
