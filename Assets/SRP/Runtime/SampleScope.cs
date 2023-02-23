using System;
using UnityEngine.Rendering;

namespace SRP.Runtime
{
	// I don't know how to use ProfilingScope, so this is a replacement for now
	public struct SampleScope : IDisposable
	{
		private readonly string _name;
		private readonly ScriptableRenderContext _context;
		private readonly CommandBuffer _buffer;

		public SampleScope(string name, ScriptableRenderContext context)
		{
			_name = name;
			_context = context;

			_buffer = CommandBufferPool.Get(_name);
			_buffer.BeginSample(_name);
			_context.ExecuteAndClearBuffer(_buffer);
		}

		public void Dispose()
		{
			_buffer.EndSample(_name);
			_context.ExecuteAndClearBuffer(_buffer);
			CommandBufferPool.Release(_buffer);
		}
	}
}