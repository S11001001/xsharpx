using System;
using System.Collections;
using System.Collections.Generic;

namespace XSharpx {
  public struct Input<E> : IEnumerable<E> {
    internal readonly Option<Option<E>> val;

    internal Input(Option<Option<E>> val) {
      this.val = val;
    }

    public bool IsEmpty {
      get {
        return val.IsEmpty;
      }
    }

    public bool IsEof {
      get {
        return val.Any(o => o.IsEmpty);
      }
    }

    public bool IsElement {
      get {
        return val.Any(o => o.IsNotEmpty);
      }
    }

    public Option<E> InputElement {
      get {
        return val.Flatten();
      }
    }

    public E InputElementOr(Func<E> f) {
      return InputElement.ValueOr(f);
    }

    public Input<E> OrElse(Func<Input<E>> i) {
      return IsElement ? this : i();
    }

    public Input<E> Append(Input<E> o, Semigroup<E> m) {
      return m.Input.Op(this, o);
    }

    public Iteratee<E, A> Done<A>(A a) {
      return Iteratee<E, A>.Done(a, this);
    }

    public Input<E> Where(Func<E, bool> p) {
      var z = InputElement;

      return IsEmpty || IsEof ? this : z.Any(p) ? this : Empty();
    }

    public Input<Input<E>> Duplicate {
      get {
        return this.Select(a => Element(a));
      }
    }

    public Input<B> Extend<B>(Func<Input<E>, B> f) {
      return this.Select(a => f(Element(a)));
    }

    public Input<C> ZipWith<B, C>(Input<B> o, Func<E, Func<B, C>> f) {
      return this.SelectMany(a => o.Select(b => f(a)(b)));
    }

    public Input<Pair<E, B>> Zip<B>(Input<B> o) {
      return ZipWith<B, Pair<E, B>>(o, Pair<E, B>.pairF());
    }

    public bool Any(Func<E, bool> p) {
      return val.Any(o => o.Any(p));
    }

    public bool All(Func<E, bool> p) {
      return val.All(o => o.All(p));
    }

    public void ForEach(Action<E> a) {
      val.ForEach(o => o.ForEach(a));
    }

    public List<E> ToList {
      get {
        return InputElement.ToList;
      }
    }

    public IEnumerator<E> GetEnumerator() {
      return val.Flatten().GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() {
      return GetEnumerator();
    }

    public List<Input<B>> TraverseList<B>(Func<E, List<B>> f) {
      return val.TraverseList(o => o.TraverseList(f)).Select(o => new Input<B>(o));
    }

    public Option<Input<B>> TraverseOption<B>(Func<E, Option<B>> f) {
      return val.TraverseOption(o => o.TraverseOption(f)).Select(o => new Input<B>(o));
    }

    public Either<X, Input<B>> TraverseEither<X, B>(Func<E, Either<X, B>> f) {
      return val.TraverseEither(o => o.TraverseEither(f)).Select(o => new Input<B>(o));
    }

    public NonEmptyList<Input<B>> TraverseNonEmptyList<B>(Func<E, NonEmptyList<B>> f) {
      return val.TraverseNonEmptyList(o => o.TraverseNonEmptyList(f)).Select(o => new Input<B>(o));
    }

    public Pair<X, Input<B>> TraversePair<X, B>(Func<E, Pair<X, B>> f, Monoid<X> m) {
      return val.TraversePair(o => o.TraversePair(f, m), m).Select(o => new Input<B>(o));
    }

    public Func<X, Input<B>> TraverseFunc<X, B>(Func<E, Func<X, B>> f) {
      return val.TraverseFunc<X, Option<B>>(o => o.TraverseFunc<X, B>(f)).Select(o => new Input<B>(o));
    }

    public Tree<Input<B>> TraverseTree<B>(Func<E, Tree<B>> f) {
      return val.TraverseTree(o => o.TraverseTree(f)).Select(o => new Input<B>(o));
    }

    public static Input<E> Empty() {
      return new Input<E>(Option.Empty);
    }

    public static Input<E> Eof() {
      return new Input<E>(Option.Some(Option<E>.Empty));
    }

    public static Input<E> Element(E e) {
      return new Input<E>(Option.Some(Option.Some(e)));
    }
  }

  public static class InputExtension {
    public static Input<B> Select<A, B>(this Input<A> k, Func<A, B> f) {
      return new Input<B>(k.val.Select(o => o.Select(f)));
    }

    public static Input<B> SelectMany<A, B>(this Input<A> k, Func<A, Input<B>> f) {
      return new Input<B>(k.val.SelectMany(o => o.SelectMany(a => f(a).val)));
    }

    public static Input<C> SelectMany<A, B, C>(this Input<A> k, Func<A, Input<B>> p, Func<A, B, C> f) {
      return SelectMany(k, a => Select(p(a), b => f(a, b)));
    }

    public static Input<B> Apply<A, B>(this Input<Func<A, B>> f, Input<A> o) {
      return f.SelectMany(g => o.Select(p => g(p)));
    }

    public static Input<A> Flatten<A>(this Input<Input<A>> o) {
      return o.SelectMany(z => z);
    }

  }

  public struct Iteratee<E, A> {
    private readonly Either<Pair<A, Input<E>>, Func<Input<E>, Iteratee<E, A>>> val;

    private Iteratee(Either<Pair<A, Input<E>>, Func<Input<E>, Iteratee<E, A>>> val) {
      this.val = val;
    }

    public Option<Pair<A, Input<E>>> DoneT {
      get {
        return val.Swap.ToOption;
      }
    }

    public Option<A> DoneA {
      get {
        return DoneT.Select(x => x._1.Get);
      }
    }

    public Option<Input<E>> DoneI {
      get {
        return DoneT.Select(x => x._2.Get);
      }
    }

    public bool IsDone {
      get {
        return DoneI.IsNotEmpty;
      }
    }

    public bool IsDoneEmpty {
      get {
        return DoneI.Any(i => i.IsEmpty);
      }
    }

    public bool IsDoneEof {
      get {
        return DoneI.Any(i => i.IsEof);
      }
    }

    public bool isDoneElement {
      get {
        return DoneI.Any(i => i.IsElement);
      }
    }

    public Option<E> InputElement {
      get {
        return DoneI.SelectMany(i => i.InputElement);
      }
    }

    public Option<Func<Input<E>, Iteratee<E, A>>> ContT {
      get {
        return val.ToOption;
      }
    }

    public bool IsCont {
      get {
        return ContT.IsNotEmpty;
      }
    }

    public Option<Iteratee<E, A>> ApplyCont(Input<E> i) {
      return ContT.Select(f => f(i));
    }

    public static Iteratee<E, A> Done(A a, Input<E> i) {
      return new Iteratee<E, A>(Pair<A, Input<E>>.pair(a, i).Left<Pair<A, Input<E>>, Func<Input<E>, Iteratee<E, A>>>());
    }

    public static Iteratee<E, A> Cont(Func<Input<E>, Iteratee<E, A>> c) {
      return new Iteratee<E, A>(c.Right<Pair<A, Input<E>>, Func<Input<E>, Iteratee<E, A>>>());
    }
  }

}
