﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Prover.CommProtocol.Common.Extensions;
using Prover.CommProtocol.Common.IO;
using Prover.CommProtocol.Common.Messaging;

namespace Prover.CommProtocol.MiHoneywell.Messaging.Response
{
    internal class ResponseProcessors
    {
        public static ResponseProcessor<StatusResponseMessage>
            ResponseCode = new ResponseCodeProcessor();

        public static ResponseProcessor<ItemValueResponseMessage>
            ItemValue(int itemNumber) => new ItemValueProcessor(itemNumber);

        public static ResponseProcessor<ItemGroupResponseMessage>
            ItemGroup(IEnumerable<int> itemNumbers) => new ItemGroupValuesProcessor(itemNumbers);
    }

    internal class ItemValueProcessor : ResponseProcessor<ItemValueResponseMessage>
    {
        public ItemValueProcessor(int itemNumber)
        {
            ItemNumber = itemNumber;
        }

        public int ItemNumber { get; }

        public override IObservable<ItemValueResponseMessage> ResponseObservable(IObservable<char> source)
        {
            return Observable.Create<ItemValueResponseMessage>(observer =>
            {
                var numberChars = new List<char>();
                var valueChars = new List<char>();
                var checksumChars = new List<char>();

                Action emitPacket = () =>
                {
                    if (valueChars.Count > 0)
                    {
                        var numberString = new string(numberChars.ToArray());
                        var valString = new string(valueChars.ToArray());
                        var checksum = new string(checksumChars.ToArray());

                        observer.OnNext(new ItemValueResponseMessage(ItemNumber, valString, checksum));

                        numberChars.Clear();
                        valueChars.Clear();
                        checksumChars.Clear();
                    }
                };

                var parsingItemNumber = false;
                var parsingItemValue = false;
                var parsingChecksum = false;

                //[SOH]123[STX]12345678[ETX]ABCD[EOT]
                return source.Subscribe(
                    c =>
                    {
                        switch (c)
                        {
                            case ControlCharacters.SOH:
                                emitPacket();
                                parsingItemNumber = true;
                                break;
                            case ControlCharacters.STX:
                                parsingItemNumber = false;
                                parsingItemValue = true;
                                break;
                            case ControlCharacters.ETX:
                                parsingItemValue = false;
                                parsingChecksum = true;
                                break;
                            case ControlCharacters.EOT:
                                emitPacket();
                                parsingItemValue = false;
                                parsingChecksum = false;
                                parsingItemNumber = false;
                                break;
                            default:
                            {
                                if (parsingItemNumber)
                                    numberChars.Add(c);

                                if (parsingItemValue)
                                    valueChars.Add(c);

                                if (parsingChecksum)
                                    checksumChars.Add(c);
                                break;
                            }
                        }
                    },
                    observer.OnError,
                    () =>
                    {
                        emitPacket();
                        observer.OnCompleted();
                    });
            });
        }
    }

    internal class ItemGroupValuesProcessor : ResponseProcessor<ItemGroupResponseMessage>
    {
        public ItemGroupValuesProcessor(IEnumerable<int> itemNumbers)
        {
            ItemNumbers = itemNumbers as int[] ?? itemNumbers.ToArray();

            if (ItemNumbers.Count() > 15)
                throw new ArgumentOutOfRangeException($"{nameof(itemNumbers)} can only have 15 items max");

            ItemValues = ItemNumbers.ToDictionary(x => x, y => string.Empty);
        }

        public IEnumerable<int> ItemNumbers { get; set; }

        private Dictionary<int, string> ItemValues { get; }

        public override IObservable<ItemGroupResponseMessage> ResponseObservable(IObservable<char> source)
        {
            return Observable.Create<ItemGroupResponseMessage>(observer =>
            {
                var currentItemNumber = ItemNumbers.GetEnumerator();
                var valueChars = new List<char>();
                var checksumChars = new List<char>();

                void EmitPacket()
                {
                    var checksum = new string(checksumChars.ToArray());

                    observer.OnNext(new ItemGroupResponseMessage(ItemValues, checksum));
                    checksumChars.Clear();
                }

                void AddValue()
                {
                    if (valueChars.Any())
                    {
                        currentItemNumber.MoveNext();
                        var raw = new string(valueChars.ToArray());
                        ItemValues[currentItemNumber.Current] = raw.ScrubInvalidCharacters();
                        
                        valueChars.Clear();
                    }
                }

                var parsingItemValue = false;
                var parsingChecksum = false;

                //[SOH]12345678,11111111[ETX]ABCD[EOT]
                return source.Subscribe(
                    c =>
                    {
                        switch (c)
                        {
                            case ControlCharacters.SOH:
                                parsingItemValue = true;
                                break;
                            case ControlCharacters.ETX:
                                AddValue();
                                parsingItemValue = false;
                                parsingChecksum = true;
                                break;
                            case ControlCharacters.EOT:
                                EmitPacket();
                                parsingItemValue = false;
                                parsingChecksum = false;
                                break;
                            case ControlCharacters.Comma:
                                AddValue();
                                break;
                            default:
                            {
                                if (parsingItemValue)
                                    valueChars.Add(c);

                                if (parsingChecksum)
                                    checksumChars.Add(c);
                                break;
                            }
                        }
                    },
                    observer.OnError,
                    () =>
                    {
                        EmitPacket();
                        observer.OnCompleted();
                    });
            });
        }
    }

    internal class ResponseCodeProcessor : ResponseProcessor<StatusResponseMessage>
    {
        public override IObservable<StatusResponseMessage> ResponseObservable(IObservable<char> source)
        {
            return Observable.Create<StatusResponseMessage>(observer =>
            {
                var codeChars = new List<char>();
                var checksumChars = new List<char>();

                Action emitPacket = () =>
                {
                    if (codeChars.Count > 0)
                    {
                        var code = int.Parse(string.Concat(codeChars.ToArray()));
                        codeChars.Clear();

                        var checksum = string.Concat(checksumChars.ToArray());
                        checksumChars.Clear();

                        observer.OnNext(new StatusResponseMessage((ResponseCode) code, checksum));
                    }
                };

                var parsingChecksum = false;
                var parsingResponseCode = false;

                return source.Subscribe(
                    c =>
                    {
                        switch (c)
                        {
                            case ControlCharacters.ACK:
                                observer.OnNext(new StatusResponseMessage(ResponseCode.NoError, "0000"));
                                break;
                            case ControlCharacters.SOH:
                                emitPacket();
                                parsingResponseCode = true;
                                break;
                            case ControlCharacters.ETX:
                                parsingResponseCode = false;
                                parsingChecksum = true;
                                break;
                            case ControlCharacters.EOT:
                                emitPacket();
                                parsingChecksum = false;
                                parsingResponseCode = false;
                                break;
                            default:
                            {
                                if (parsingResponseCode)
                                    codeChars.Add(c);

                                if (parsingChecksum)
                                    checksumChars.Add(c);
                                break;
                            }
                        }
                    },
                    observer.OnError,
                    () =>
                    {
                        emitPacket();
                        observer.OnCompleted();
                    });
            });
        }
    }
}