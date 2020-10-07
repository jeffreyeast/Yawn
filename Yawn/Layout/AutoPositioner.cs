//  Copyright (c) 2020 Jeff East
//
//  Licensed under the Code Project Open License (CPOL) 1.02
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yawn
{
    internal static class AutoPositioner
    {
        internal interface IAutoPositionerClient
        {
            bool IsStretchable(LayoutContext element);
            double GetAvailableSpace(Silo silo, LayoutContext element, double baseCoordinate, double totalAvailableSpace, List<LayoutContext> elementsToBePositioned);
            LayoutContext GetNextDescendant(Silo silo, LayoutContext element, List<LayoutContext> elementsToBePositioned);
            double GetDesiredSpace(LayoutContext element, double minimumSize);
            double GetPeersMaxDesiredSpace(LayoutContext element, double minimumSize, IEnumerable<LayoutContext> elementsToBePositioned);
            double GetPreceedingCoordinate(LayoutContext element);
            int GetRemainingDepth(Silo silo, LayoutContext element, List<LayoutContext> elementsToBePositioned);
            double GetVaryingSpace(Silo silo, double availableSpace, double minSpacePerElement, List<LayoutContext> varyingElements, List<LayoutContext> elementsToBePositioned);
            void SetPosition(Silo silo, LayoutContext element, double coordinate, double size, List<LayoutContext> elementsToBePositioned);
        }

        internal enum States
        {
            Starting,

            EndVaryingRun,
            FirstFixedElement,
            FirstVaryingElement,
            ResetAtEnd,
            ScanningVarying,
            TestingElementType,

            Finished,
        }


        internal static void Run(Silo silo, double totalAvailableSpace, double minimumSize, IAutoPositionerClient client)
        {
            States currentState = silo.FirstOrDefault() == null ? States.Finished : States.Starting;
            List<LayoutContext> elementsToBePositioned = new List<LayoutContext>(silo);
            LayoutContext currentElement = null;
            LayoutContext nextElement = null;
            List<LayoutContext> varyingRun = new List<LayoutContext>();
            double availableSpace;
            double runningCoordinate = 0;

            while (currentState != States.Finished)
            {
                switch (currentState)
                {
                    case States.EndVaryingRun:
                        availableSpace = client.GetAvailableSpace(silo, varyingRun.First(), runningCoordinate, totalAvailableSpace, elementsToBePositioned);
                        double varyingSize = client.GetVaryingSpace(silo, availableSpace, minimumSize, varyingRun, elementsToBePositioned);
                        int varyingCount = varyingRun.Count;
                        foreach (LayoutContext element in varyingRun)
                        {
                            double elementSize = varyingSize / varyingRun.Count;
                            client.SetPosition(silo, element, runningCoordinate, elementSize, elementsToBePositioned);
                            runningCoordinate += elementSize;
                            availableSpace -= elementSize;
                        }
                        if (currentElement == null)
                        {
                            if (varyingCount > 0 && availableSpace > 0.1)           //  Round-off error
                            {
                                throw new InvalidOperationException("getVaryingSpace did not consume all the available space");
                            }
                            currentState = States.ResetAtEnd;
                        }
                        else
                        {
                            currentState = States.TestingElementType;
                        }
                        continue;

                    case States.Finished:
                        continue;

                    case States.FirstFixedElement:
                        double desiredSpace = client.GetPeersMaxDesiredSpace(currentElement, minimumSize, silo);
                        elementsToBePositioned.Remove(currentElement);
                        int remainingDepth = client.GetRemainingDepth(silo, currentElement, elementsToBePositioned);
                        availableSpace = client.GetAvailableSpace(silo, currentElement, runningCoordinate, totalAvailableSpace, elementsToBePositioned);
                        if (availableSpace - desiredSpace < remainingDepth * minimumSize)
                        {
                            desiredSpace = Math.Max(0, Math.Max(availableSpace - remainingDepth * minimumSize, availableSpace / ( 1 + remainingDepth)));
                        }
                        client.SetPosition(silo, currentElement, runningCoordinate, desiredSpace, elementsToBePositioned);

                        nextElement = client.GetNextDescendant(silo, currentElement, elementsToBePositioned);

                        if (nextElement == null)
                        {
                            if (availableSpace > 0)
                            {
                                client.SetPosition(silo, currentElement, runningCoordinate, availableSpace, elementsToBePositioned);
                            }
                            currentState = States.ResetAtEnd;
                        }
                        else
                        {
                            runningCoordinate += desiredSpace;
                            currentElement = nextElement;
                            currentState = States.TestingElementType;
                        }
                        continue;

                    case States.FirstVaryingElement:
                        varyingRun.Clear();
                        currentState = States.ScanningVarying;
                        continue;

                    case States.ResetAtEnd:
                        if (elementsToBePositioned.Count == 0)
                        {
                            currentState = States.Finished;
                        }
                        else
                        {
                            currentElement = elementsToBePositioned[0];
                            runningCoordinate = client.GetPreceedingCoordinate(currentElement);
                            currentState = States.TestingElementType;
                        }
                        continue;

                    case States.ScanningVarying:
                        varyingRun.Add(currentElement);
                        elementsToBePositioned.Remove(currentElement);
                        currentElement = client.GetNextDescendant(silo, currentElement, elementsToBePositioned);
                        if (currentElement == null)
                        {
                            currentState = States.EndVaryingRun;
                        }
                        else
                        {
                            currentState = client.IsStretchable(currentElement) ? States.ScanningVarying : States.EndVaryingRun;
                        }
                        continue;

                    case States.Starting:
                        currentElement = elementsToBePositioned[0];
                        currentState = States.TestingElementType;
                        runningCoordinate = 0;
                        continue;

                    case States.TestingElementType:
                        currentState = client.IsStretchable(currentElement) ? States.FirstVaryingElement : States.FirstFixedElement;
                        continue;
                }
            }
        }
    }
}
