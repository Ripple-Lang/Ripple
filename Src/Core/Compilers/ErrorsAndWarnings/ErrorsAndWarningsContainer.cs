using System.Collections.Generic;

namespace Ripple.Compilers.ErrorsAndWarnings
{
    public interface IErrorsAndWarningsContainer : IEnumerable<ErrorAndWarning>
    {
        /// <summary>
        /// このオブジェクトに格納されているエラーを取得します。
        /// </summary>
        IEnumerable<Error> Errors { get; }

        /// <summary>
        /// このオブジェクトに格納されている警告を取得します。
        /// </summary>
        IEnumerable<Warning> Warnings { get; }

        /// <summary>
        /// このオブジェクトにエラーが格納されているかを示す値を取得します。
        /// </summary>
        bool HasErrors { get; }

        /// <summary>
        /// このオブジェクトに警告が格納されているかを示す値を取得します。
        /// </summary>
        bool HasWarnings { get; }

        /// <summary>
        /// このオブジェクトにエラーまたは警告が格納されているかを示す値を取得します。
        /// </summary>
        bool HasErrorsOrWarnings { get; }
    }

    /// <summary>
    /// エラーと警告を格納します。
    /// </summary>
    public sealed class ErrorsAndWarningsContainer : IErrorsAndWarningsContainer
    {
        private readonly List<Error> errors;
        private readonly List<Warning> warnings;

        /// <summary>
        /// このオブジェクトに格納されているエラーを取得します。
        /// </summary>
        public IEnumerable<Error> Errors { get { return errors.AsReadOnly(); } }

        /// <summary>
        /// このオブジェクトに格納されている警告を取得します。
        /// </summary>
        public IEnumerable<Warning> Warnings { get { return warnings.AsReadOnly(); } }

        /// <summary>
        /// このオブジェクトにエラーが格納されているかを示す値を取得します。
        /// </summary>
        public bool HasErrors { get { return errors.Count != 0; } }

        /// <summary>
        /// このオブジェクトに警告が格納されているかを示す値を取得します。
        /// </summary>
        public bool HasWarnings { get { return warnings.Count != 0; } }

        /// <summary>
        /// このオブジェクトにエラーまたは警告が格納されているかを示す値を取得します。
        /// </summary>
        public bool HasErrorsOrWarnings { get { return HasErrors || HasWarnings; } }

        public ErrorsAndWarningsContainer()
        {
            this.errors = new List<Error>();
            this.warnings = new List<Warning>();
        }

        public ReadonlyErrorsAndWarningsContainer AsReadonly()
        {
            return new ReadonlyErrorsAndWarningsContainer(this);
        }

        internal void AddError(Error error)
        {
            this.errors.Add(error);
        }

        internal void AddWarning(Warning warning)
        {
            warnings.Add(warning);
        }

        public IEnumerator<ErrorAndWarning> GetEnumerator()
        {
            foreach (var item in errors)
            {
                yield return item;
            }

            foreach (var item in warnings)
            {
                yield return item;
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public sealed class ReadonlyErrorsAndWarningsContainer : IErrorsAndWarningsContainer
    {
        private readonly ErrorsAndWarningsContainer container;

        public ReadonlyErrorsAndWarningsContainer(ErrorsAndWarningsContainer container)
        {
            this.container = container;
        }

        public IEnumerable<Error> Errors
        {
            get { return container.Errors; }
        }

        public IEnumerable<Warning> Warnings
        {
            get { return container.Warnings; }
        }

        public bool HasErrors
        {
            get { return container.HasErrors; }
        }

        public bool HasWarnings
        {
            get { return container.HasWarnings; }
        }

        public bool HasErrorsOrWarnings
        {
            get { return container.HasErrorsOrWarnings; }
        }

        public IEnumerator<ErrorAndWarning> GetEnumerator()
        {
            return container.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return container.GetEnumerator();
        }
    }
}
