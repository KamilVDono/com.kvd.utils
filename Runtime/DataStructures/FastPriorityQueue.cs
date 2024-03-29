using System;
using System.Runtime.CompilerServices;
using Unity.Collections;

namespace KVD.Utils.DataStructures
{
	[GenerateTestsForBurstCompatibility]
	public struct FastPriorityQueue<T> : IDisposable where T : unmanaged, IEquatable<T>
	{
		private uint _numNodes;
		private UnsafeArray<T> _nodes;
		private NativeHashMap<T, NodeData> _dataByNode;
		
		public uint Count => _numNodes;

		public uint MaxSize => _nodes.Length-1;

		public FastPriorityQueue(uint maxNodes, Allocator allocator = Allocator.Temp)
		{
			_numNodes   = 0;
			_nodes      = new(maxNodes, allocator);
			_dataByNode = new((int)maxNodes, allocator);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Contains(T node)
		{
			return _dataByNode.ContainsKey(node);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Enqueue(T node, float priority)
		{
			_numNodes++;
			_nodes[_numNodes] = node;
			var nodeData = new NodeData
			{
				priority   = priority,
				queueIndex = _numNodes
			};
			_dataByNode.Add(node, nodeData);
			CascadeUp(node, nodeData);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public T Dequeue()
		{
			var returnMe = _nodes[1];
			_dataByNode.Remove(returnMe);
			//If the node is already the last node, we can remove it immediately
			if (_numNodes == 1)
			{
				_numNodes = 0;
				return returnMe;
			}

			//Swap the node with the last node
			var formerLastNode = _nodes[_numNodes];
			var lastNodeData   = _dataByNode[formerLastNode];
			_nodes[1]                   = formerLastNode;
			lastNodeData.queueIndex     = 1;
			_dataByNode[formerLastNode] = lastNodeData;
			_numNodes--;

			//Now bubble formerLastNode (which is no longer the last node) down
			CascadeDown(formerLastNode, lastNodeData);
			return returnMe;
		}
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void UpdatePriority(T node, float priority)
		{
			var data = _dataByNode[node];
			data.priority     = priority;
			_dataByNode[node] = data;
			OnNodeUpdated(node, data);
		}
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Remove(T node)
		{
			var data = _dataByNode[node];
			//If the node is already the last node, we can remove it immediately
			if (data.queueIndex == _numNodes)
			{
				_dataByNode.Remove(node);
				_numNodes--;
				return;
			}

			//Swap the node with the last node
			var formerLastNode = _nodes[_numNodes];
			var lastNodeData   = _dataByNode[formerLastNode];
			_nodes[data.queueIndex]     = formerLastNode;
			lastNodeData.queueIndex     = data.queueIndex;
			_dataByNode[formerLastNode] = lastNodeData;
			_dataByNode.Remove(node);
			_numNodes--;

			//Now bubble formerLastNode (which is no longer the last node) up or down as appropriate
			OnNodeUpdated(formerLastNode, lastNodeData);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Clear()
		{
			_numNodes = 0;
			_dataByNode.Clear();
		}

		public bool IsValidQueue()
		{
			for (var i = 1u; i < _nodes.Length; i++)
			{
				var childLeftIndex = 2*i;
				if (childLeftIndex < _nodes.Length && HasHigherPriority(_dataByNode[_nodes[childLeftIndex]], _dataByNode[_nodes[i]]))
				{
					return false;
				}

				var childRightIndex = childLeftIndex+1;
				if (childRightIndex < _nodes.Length && HasHigherPriority(_dataByNode[_nodes[childRightIndex]], _dataByNode[_nodes[i]]))
				{
					return false;
				}
				
			}
			return true;
		}
		
		public void Dispose()
		{
			_nodes.Dispose();
			_dataByNode.Dispose();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void OnNodeUpdated(T node, NodeData data)
		{
			//Bubble the updated node up or down as appropriate
			var parentIndex = data.queueIndex >> 1;

			if (parentIndex > 0 && HasHigherPriority(data, _dataByNode[_nodes[parentIndex]]))
			{
				CascadeUp(node, data);
			}
			else
			{
				//Note that CascadeDown will be called if parentNode == node (that is, node is the root)
				CascadeDown(node, data);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void CascadeUp(T node, NodeData data)
		{
			//aka Heapify-up
			uint parent;
			if (data.queueIndex > 1)
			{
				parent = data.queueIndex >> 1;
				var parentNode = _nodes[parent];
				var parentData = _dataByNode[parentNode];
				if (HasHigherOrEqualPriority(parentData, data))
				{
					return;
				}

				//Node has lower priority value, so move parent down the heap to make room
				_nodes[data.queueIndex] = parentNode;
				parentData.queueIndex   = data.queueIndex;
				_dataByNode[parentNode] = parentData;

				data.queueIndex = parent;
			}
			else
			{
				return;
			}
			
			while (parent > 1)
			{
				parent >>= 1;
				var parentNode = _nodes[parent];
				var parentData = _dataByNode[parentNode];
				if (HasHigherOrEqualPriority(parentData, data))
				{
					break;
				}

				//Node has lower priority value, so move parent down the heap to make room
				_nodes[data.queueIndex] = parentNode;
				parentData.queueIndex   = data.queueIndex;
				_dataByNode[parentNode] = parentData;

				data.queueIndex = parent;
			}
			_dataByNode[node]       = data;
			_nodes[data.queueIndex] = node;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void CascadeDown(T node, NodeData data)
		{
			//aka Heapify-down
			var finalQueueIndex = data.queueIndex;
			var childLeftIndex  = 2*finalQueueIndex;

			// If leaf node, we're done
			if (childLeftIndex > _numNodes)
			{
				return;
			}

			// Check if the left-child is higher-priority than the current node
			var childRightIndex = childLeftIndex+1;
			var childLeft       = _nodes[childLeftIndex];
			var childLeftData   = _dataByNode[childLeft];
			if (HasHigherPriority(childLeftData, data))
			{
				// Check if there is a right child. If not, swap and finish.
				if (childRightIndex > _numNodes)
				{
					data.queueIndex          = childLeftIndex;
					_dataByNode[node]        = data;
					childLeftData.queueIndex = finalQueueIndex;
					_dataByNode[childLeft]   = childLeftData;
					_nodes[finalQueueIndex]  = childLeft;
					_nodes[childLeftIndex]   = node;
					return;
				}
				// Check if the left-child is higher-priority than the right-child
				var childRight     = _nodes[childRightIndex];
				var childRightData = _dataByNode[childRight];
				if (HasHigherPriority(childLeftData, childRightData))
				{
					// left is highest, move it up and continue
					childLeftData.queueIndex = finalQueueIndex;
					_dataByNode[childLeft]   = childLeftData;
					_nodes[finalQueueIndex]  = childLeft;
					finalQueueIndex          = childLeftIndex;
				}
				else
				{
					// right is even higher, move it up and continue
					childRightData.queueIndex = finalQueueIndex;
					_dataByNode[childRight]   = childRightData;
					_nodes[finalQueueIndex]   = childRight;
					finalQueueIndex           = childRightIndex;
				}
			}
			// Not swapping with left-child, does right-child exist?
			else if (childRightIndex > _numNodes)
			{
				return;
			}
			else
			{
				// Check if the right-child is higher-priority than the current node
				var childRight     = _nodes[childRightIndex];
				var childRightData = _dataByNode[childRight];
				if (HasHigherPriority(childRightData, data))
				{
					childRightData.queueIndex = finalQueueIndex;
					_dataByNode[childRight]   = childRightData;
					_nodes[finalQueueIndex]   = childRight;
					finalQueueIndex           = childRightIndex;
				}
				// Neither child is higher-priority than current, so finish and stop.
				else
				{
					return;
				}
			}

			while (true)
			{
				childLeftIndex = 2*finalQueueIndex;

				// If leaf node, we're done
				if (childLeftIndex > _numNodes)
				{
					data.queueIndex         = finalQueueIndex;
					_dataByNode[node]       = data;
					_nodes[finalQueueIndex] = node;
					break;
				}

				// Check if the left-child is higher-priority than the current node
				childRightIndex = childLeftIndex+1;
				childLeft       = _nodes[childLeftIndex];
				childLeftData   = _dataByNode[childLeft];
				if (HasHigherPriority(childLeftData, data))
				{
					// Check if there is a right child. If not, swap and finish.
					if (childRightIndex > _numNodes)
					{
						data.queueIndex          = childLeftIndex;
						_dataByNode[node]        = data;
						childLeftData.queueIndex = finalQueueIndex;
						_dataByNode[childLeft]   = childLeftData;
						_nodes[finalQueueIndex]  = childLeft;
						_nodes[childLeftIndex]   = node;
						break;
					}
					// Check if the left-child is higher-priority than the right-child
					var childRight     = _nodes[childRightIndex];
					var childRightData = _dataByNode[childRight];
					if (HasHigherPriority(childLeftData, childRightData))
					{
						// left is highest, move it up and continue
						childLeftData.queueIndex = finalQueueIndex;
						_dataByNode[childLeft]   = childLeftData;
						_nodes[finalQueueIndex]  = childLeft;
						finalQueueIndex          = childLeftIndex;
					}
					else
					{
						// right is even higher, move it up and continue
						childRightData.queueIndex = finalQueueIndex;
						_dataByNode[childRight]   = childRightData;
						_nodes[finalQueueIndex]   = childRight;
						finalQueueIndex           = childRightIndex;
					}
				}
				// Not swapping with left-child, does right-child exist?
				else if (childRightIndex > _numNodes)
				{
					data.queueIndex         = finalQueueIndex;
					_dataByNode[node]       = data;
					_nodes[finalQueueIndex] = node;
					break;
				}
				else
				{
					// Check if the right-child is higher-priority than the current node
					var childRight     = _nodes[childRightIndex];
					var childRightData = _dataByNode[childRight];
					if (HasHigherPriority(childRightData, data))
					{
						childRightData.queueIndex = finalQueueIndex;
						_dataByNode[childRight]   = childRightData;
						_nodes[finalQueueIndex]   = childRight;
						finalQueueIndex           = childRightIndex;
					}
					// Neither child is higher-priority than current, so finish and stop.
					else
					{
						data.queueIndex         = finalQueueIndex;
						_dataByNode[node]       = data;
						_nodes[finalQueueIndex] = node;
						break;
					}
				}
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool HasHigherPriority(NodeData higher, NodeData lower)
		{
			return higher.priority < lower.priority;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool HasHigherOrEqualPriority(NodeData higher, NodeData lower)
		{
			return higher.priority <= lower.priority;
		}

		private struct NodeData
		{
			public float priority;
			public uint queueIndex;
		}
	}
}
