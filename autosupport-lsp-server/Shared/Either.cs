using System;
using System.Collections.Generic;
using System.Text;

namespace autosupport_lsp_server.Shared
{
    public class Either<TLeft, TRight>
    {
        private readonly bool isLeft;

        private readonly TLeft left;
        private readonly TRight right;

        public static implicit operator Either<TLeft, TRight>(TLeft left) => new Either<TLeft, TRight>(left);

        public static implicit operator Either<TLeft, TRight>(TRight right) => new Either<TLeft, TRight>(right);

#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
        public Either(TLeft left)
        {
            isLeft = true;
            this.left = left;
        }

        public Either(TRight right)
        {
            isLeft = false;
            this.right = right;
        }
#pragma warning restore CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.

        public void Match(Action<TLeft> leftFunc, Action<TRight> rightFunc)
        {
            if (isLeft)
                leftFunc.Invoke(left);
            else
                rightFunc.Invoke(right);
        }

        public R Match<R>(Func<TLeft, R> leftFunc, Func<TRight, R> rightFunc) => isLeft ? leftFunc.Invoke(left) : rightFunc.Invoke(right);
    }
}
