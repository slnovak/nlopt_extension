using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Threading;

using Aisolutions.FreeFlyer.SDK;

using NLOptWrapper;

namespace NLOptExtension
{
	[ComVisible(true)]
	[ClassInterface(ClassInterfaceType.None)]
	[Guid("e4a87040-048f-45f4-97f2-1a562d2c160c")]
	public class OptimizerExtension : ExtensionBase, 
                                      IFFObjectExtension, 
    								  IOptimizerExtension
	{
		private double _maxTime;
		private int _maxEvaluations;
		private double _stopValue;

		private double[] _dataVector;
		private double[] _lowerBounds;
		private double[] _upperBounds;

		private double _functionAbsoluteTolerance;
		private double _functionRelativeTolerance;
		private double _parameterRelativeTolerance;
		private double[] _parameterAbsoluteTolerance;

		private string _currentConstraintFunctionLabel;
		private bool _isCalculatingObjectiveFunction;

		private AutoResetEvent _constraintFunctionEvent;
		private AutoResetEvent _objectiveFunctionEvent;
		private Result _result;
		private Thread _optimizationThread;

		private double _constraintFunctionValue;
		private double _objectiveFunctionValue;

		// TODO: Add _constraint/_objective function gradient

		private NLOptWrapper _optimizer;

		#region ISimpleExtension Members

		public override void Initialize(IFFHost host, IFFObject baseObject)
		{
			base.Initialize(host, baseObject);
			_isCalculatingObjectiveFunction = false;
		}

		public void Configure(string algorithmType, int size, string objectiveType)
		{
			Algorithm alg = Enum.Parse(typeof(Algorithm), algorithmType.ToUpper());
			_optimizer = new NLOptWrapper(alg, size);

			// Parse out objectiveType. It can be either a string for
			// MINIMUM_OBJECTIVE or MAXIMUM_OBJECTIVE
			FunctionType function = Enum.Parse(typeof(FunctionType), objectiveType.ToUpper());
			_optimizer.AddFunction(function, _objectiveFunction, null, 0.0);	
		}

		public void GetInputArray(out double[] data)
		{
			Array.Copy(_dataVector, data, _dataVector.Length);
		}

		public void SetInputArray(double[] data)
		{
			Array.Copy(data, _dataVector, data.Length);
		}

		public void SetUpperBounds(double[] data)
		{
			_upperBounds = new double[data.Length];
			Array.Copy(data, _upperBounds, data.Length);
		}

		public void SetLowerBounds(double[] data)
		{
			_lowerBounds = new double[data.Length];
			Array.Copy(data, _lowerBounds, data.Length);
		}

		public bool IsCalculatingObjectiveFunction()
		{
			return _isCalculatingObjectiveFunction;
		}

		public bool IsCalculatingConstraintFunction(string constraintName)
		{
			return _currentConstraintFunctionLabel == constraintName;
		}

		public void RegisterConstraintFunction(string label, string functionType, double tolerance)
		{
			FunctionType function = Enum.Parse(typeof(FunctionType), functionType.ToUpper());
			_optimizer.AddFunction(function, _constraintFunction, label, tolerance);	
		}

		public void Start()
		{
			_objectiveFunctionEvent = new AutoResetEvent(false);
			_constraintFunctionEvent = new AutoResetEvent(false);

			_optimizationThread = new Thread(_runOptimizer);
			_optimizationThread.Start();
		}

		void SetParameterAbsoluteTolerance(double[] data)
		{
			_parameterAbsoluteTolerance = new double[data.Length];
			Array.Copy(data, _parameterAbsoluteTolerance, data.Length);
		}

		public bool IsRunning()
		{
			_optimizationThread.IsAlive;
		}

		private double _objectiveFunction(double[] x, ref double[] grad, Object data)
		{
			Array.Copy(x, _dataVector, x.Length);

			_isCalculatingObjectiveFunction = true;

			// Wait for _objectiveFunctionEvent.Set() to be called.
			// Meanwhile, the FF mission plan should set ObjectiveFunctionValue
			// to a value.
			_objectiveFunctionEvent.WaitOne();

			_isCalculatingObjectiveFunction = false;

			return _objectiveFunctionValue;
		}

		private void _constraintFunction(double[] x, ref double[] grad, Object data)
		{
			Array.Copy(x, _dataVector, x.Length);

			// TODO: copy grad to a private member so that FF can access the gradient

			_currentConstraintFunctionLabel = (String)data;

			// Wait for _constraintFunctionEvent.Set() to be called.
			// Meanwhile, the FF mission plan should set ConstraintFunctionValue
			// to a value.
			_constraintFunctionEvent.WaitOne();

			_currentConstraintFunctionLabel = null;

			return _constraintFunctionValue;
		}

		private void _runOptimizer()
		{
			double minf = 0;
			_result = _wrapper.Optimize(_dataVector, ref minf);
		}

		#endregion

		#region Extension Properties

		public double ConstraintFunctionValue {
			get { return _constraintFunctionValue; }
			set { 
				_constraintFunctionValue = value; 
				_constraintFunctionEvent.Set();
			}
		}

		public double ObjectiveFunctionValue {
			get { return _objectiveFunctionValue; }
			set {
				_objectiveFunctionValue = value;
				_objectiveFunctionEvent.Set();
			}
		}

		public double FunctionRelativeTolerance { 
			get { return _functionRelativeTolerance; }
			set { _functionRelativeTolerance = value; }
		}

		public double FunctionAbsoluteTolerance {
			get { return _functionAbsoluteTolerance; }
			set { _functionAbsoluteTolerance = value; }
		}

		public int MaxEvaluations {
			get { return _maxEvaluations; }
			set { _maxEvaluations = value; }
		}

		public double MaxTime { 
			get { return _maxTime; }
			set { _maxTime = value; }
		}

		public double ParameterRelativeTolerance {
			get { return _parameterRelativeTolerance; }
			set { _parameterRelativeTolerance = value; }
		}

		public double StopValue {
			get { return _stopValue; }
			set { _stopValue = value; }
		}

		public string Results {
			get { return _result.ToString(); }
			set { }
		}

		#endregion

		#region Extension Configuration

		public override Type PublishedInterfaceType
		{
			get { return typeof(IOptimizer); }
		}

		public override string ExtendedTypeName
		{
			get { return "Optimizer"; }
		}

		#endregion

		#region COM Category Reg

		[ComRegisterFunction]
		private static void ComRegisterFunction(Type t)
		{
			FFExtensionUtils.RegForComUse_V1(t);
		}

		[ComUnregisterFunction]
		private static void ComUnregisterFunction(Type t)
		{
			FFExtensionUtils.UnRegForComUse_V1(t);
		}
		#endregion
	}
}
