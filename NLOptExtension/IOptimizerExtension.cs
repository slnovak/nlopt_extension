using System;

namespace NLOptExtension
{
	public interface IOptimizerExtension
	{
		void Configure(string algorithmType, int size);

		double MaxTime { get; set; }
		int MaxEvaluations { get; set; }
		double StopValue { get; set; }

		string CurrentConstraintFunctionLabel { get; set; }

		double ObjectiveFunctionValue { get; set; }
		double CurrentConstraintFunctionValue { get; set; }

		double FunctionRelativeTolerance { get; set; }
		double FunctionAbsoluteTolerance { get; set; }
		double ParameterRelativeTolerance { get; set; }

		void GetParameterAbsoluteTolerance(out double[] data);
		void SetParameterAbsoluteTolerance(double[] data);

		bool IsCalculatingConstraintFunction(string constraintLabel);
		bool IsCalculatingObjectiveFunction();
		bool IsRunning();

		void SetInputArray(double[] data);
		void GetInputArray(out double[] data);
		void SetUpperBounds(double[] bounds);
		void SetLowerBounds(double[] bounds);

		void Start();

		string Result {get; set;}
	}
}