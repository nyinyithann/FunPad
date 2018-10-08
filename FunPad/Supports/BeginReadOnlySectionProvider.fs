namespace FunPad

open ICSharpCode.AvalonEdit.Document
open ICSharpCode.AvalonEdit.Editing

type BeginReadOnlySectionProvider() =
        let mutable endOfOffset = 0
        member self.EndOffset
            with get() =  endOfOffset
            and set v = endOfOffset <- v
        interface IReadOnlySectionProvider with
            member self.CanInsert offset = offset >= self.EndOffset
            member self.GetDeletableSegments (segment : ISegment) =
                if segment.EndOffset < self.EndOffset then Seq.empty<ISegment>
                else
                    Seq.singleton ((new TextSegment(StartOffset = System.Math.Max(self.EndOffset, segment.Offset),
                                        EndOffset = segment.EndOffset)) :> ISegment)

