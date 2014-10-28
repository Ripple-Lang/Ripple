using Ripple.Compilers.Types;

namespace Ripple.Compilers.ConstantValues
{
    internal static class Constants
    {
        #region コンパイル後のシンボル名

        public const string NowVariableName = "__now";
        public const string OperationMethodName = "__Operate";
        //public const string ComputeNextMethodName = "__ComputeNext";
        public const string InitializeMethodName = "__Initialize";
        public const string UserInitializeMethodName = "__UserInitialize";
        public const string MoveNextMethodName = "__MoveNext";
        public const string RunMethodName = "__Run";
        public const string MaxTimeVariableName = "__maxtime";

        #endregion

        public static readonly TypeData TimeType = BuiltInNumericType.Int32;
        public static readonly TypeData IndexerType = BuiltInNumericType.Int32;

        #region コンポーネント関連

        public const string ComponentDllFileName = @"Ripple.Core.Components.dll";
        public const string ComponentNameSpace = @"Ripple.Components";
        public const string IStageContainerTypeFullName = ComponentNameSpace + "." + @"IStageContainer";
        public const string AllHoldedStageContainerFullName = ComponentNameSpace + "." + @"AllHoldedStageContainer";
        public const string PartiallyHoldedStageContainerFullName = ComponentNameSpace + "." + @"PartiallyHoldedStageContainer";
        public const string ContainerNowPropertyName = "Now";
        public const string ContainerNextPropertyName = "Next";
        public const string ISimulationFullName = "Ripple.Components.ISimulation";
        public const string OnTimeChangedEventHandlerName = "Ripple.Components.__OnTimeChangedEventHandler";
        public const string OnTimeChangedEventName = "__OnTimeChanged";

        #endregion
    }
}
