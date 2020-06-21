using System;
using System.Collections.Generic;

namespace autosupport_lsp_server.Shared
{
    public class Either<TLeft, TRight>
    {
        private readonly bool isLeft;

        private readonly TLeft left;
        private readonly TRight right;

        public static implicit operator Either<TLeft, TRight>(TLeft left) => new Either<TLeft, TRight>(left);

        public static implicit operator Either<TLeft, TRight>(TRight right) => new Either<TLeft, TRight>(right);

        public static bool operator ==(Either<TLeft, TRight> either, TLeft other) => either.isLeft && either.left!.Equals(other);
        public static bool operator !=(Either<TLeft, TRight> either, TLeft other) => !either.isLeft || !either.left!.Equals(other);

        public static bool operator ==(Either<TLeft, TRight> either, TRight other) => !either.isLeft && either.right!.Equals(other);
        public static bool operator !=(Either<TLeft, TRight> either, TRight other) => either.isLeft || !either.right!.Equals(other);

        public static bool operator ==(TLeft other, Either<TLeft, TRight> either) => either.isLeft && either.left!.Equals(other);
        public static bool operator !=(TLeft other, Either<TLeft, TRight> either) => !either.isLeft || !either.left!.Equals(other);

        public static bool operator ==(TRight other, Either<TLeft, TRight> either) => !either.isLeft && either.right!.Equals(other);
        public static bool operator !=(TRight other, Either<TLeft, TRight> either) => either.isLeft || !either.right!.Equals(other);

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

        public override bool Equals(object? obj)
        {
            return obj is Either<TLeft, TRight> either &&
                   isLeft == either.isLeft &&
                   EqualityComparer<TLeft>.Default.Equals(left, either.left) &&
                   EqualityComparer<TRight>.Default.Equals(right, either.right);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(isLeft, left, right);
        }

        public override string? ToString()
        {
            return Match(l => $"{l}", r => $"{r}");
        }
    }

    public static class Either
    {
        public static Either<TLeft, TRight> If<TLeft, TRight>(bool condition, TLeft left, TRight right)
        {
            if (condition)
                return left;
            else
                return right;
        }

        public static Either<TLeft, TRight> If<TLeft, TRight>(bool condition, Func<TLeft> left, Func<TRight> right)
        {
            if (condition)
                return left.Invoke();
            else
                return right.Invoke();
        }
    }
}
