using Devices.Communications.IO;
using Devices.Communications.Messaging;
using Devices.Core.Interfaces;
using Devices.Honeywell.Comm.Messaging.Response;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Devices.Honeywell.Comm.Messaging.Requests
{
    internal static class Commands
    {
        #region Fields

        public const string DefaultAccessCode = "33333";

        #endregion

        #region Methods

        /// <summary>
        /// Creates a Live Read command
        /// </summary>
        /// <param name="itemNumber">Item number to live read</param>
        /// <returns></returns>
        public static MiCommandDefinition<ItemValueResponseMessage>
            LiveReadItem(int itemNumber) => new LiveReadItemCommand(itemNumber);

        /// <summary>
        /// Creates an NAK request Used when the instrument isn't responding to the first two wake
        /// ups A last effort to try and wake the instrument up
        /// </summary>
        /// <returns>No response is expected</returns>
        public static MiCommandDefinition<StatusResponseMessage>
            OkayToSend() => new MiCommandDefinition<StatusResponseMessage>(ControlCharacters.NAK);

        /// <summary>
        /// Creates a Read Group command to get a group of values
        /// </summary>
        /// <param name="itemNumbers">A collection of 15 item numbers maximum</param>
        /// <returns></returns>
        /// <exception cref=""></exception>
        public static MiCommandDefinition<ItemGroupResponseMessage>
            ReadGroup(IEnumerable<int> itemNumbers) => new ReadGroupCommand(itemNumbers);

        /// <summary>
        /// Creates a Read Item command to get a single value
        /// </summary>
        /// <param name="itemNumber">Item number to request</param>
        /// <returns></returns>
        public static MiCommandDefinition<ItemValueResponseMessage>
            ReadItem(int itemNumber) => new ReadItemCommand(itemNumber);

        /// <summary>
        /// Creates a Sign Off command
        /// </summary>
        /// <returns>Expects a response code in return NoError indicates we're disconnected cleanly</returns>
        public static MiCommandDefinition<StatusResponseMessage>
            SignOffCommand() => new MiCommandDefinition<StatusResponseMessage>("SF", ResponseProcessors.ResponseCode);

        /// <summary>
        /// Creates the Sign On command to the instrument
        /// </summary>
        /// <param name="evcType">Instrument Type</param>
        /// <param name="accessCode">Password for access to the instrument</param>
        /// <returns>A response code is expected in return NoError indicates we're connected</returns>
        public static MiCommandDefinition<StatusResponseMessage>
            SignOn(IEvcDeviceType evcType, string accessCode = null)
        {
            if (string.IsNullOrEmpty(accessCode))
                accessCode = DefaultAccessCode;

            var code = evcType.AccessCode < 10 ? string.Concat("0", evcType.AccessCode) : evcType.AccessCode.ToString();
            var cmd = $"SN,{accessCode}{ControlCharacters.STX}vq{code}";
            return new MiCommandDefinition<StatusResponseMessage>(cmd, ResponseProcessors.ResponseCode);
        }

        /// <summary>
        /// Creates an EOT request for the instrument Initiates the first part of the handshake
        /// Waking the instrument up
        /// </summary>
        /// <returns>No response is expected from the instrument</returns>
        public static MiCommandDefinition<StatusResponseMessage>
            WakeupOne() => new MiCommandDefinition<StatusResponseMessage>(ControlCharacters.EOT);

        /// <summary>
        /// Creates an ENQ request for the instrument Second sequence of the handshake
        /// </summary>
        /// <returns>A response code is expected in return - ACK if successful</returns>
        public static MiCommandDefinition<StatusResponseMessage>
            WakeupTwo()
            => new MiCommandDefinition<StatusResponseMessage>(ControlCharacters.ENQ, ResponseProcessors.ResponseCode);

        /// <summary>
        /// Creates a Write Item command to set a value
        /// </summary>
        /// <param name="itemNumber">Item number to write</param>
        /// <param name="value">Value to write</param>
        /// <param name="accessCode">Password for the instrument</param>
        /// <returns>A response code is expected in return</returns>
        public static MiCommandDefinition<StatusResponseMessage>
            WriteItem(int itemNumber, string value, string accessCode = DefaultAccessCode)
            => new WriteItemCommand(itemNumber, value, accessCode);

        #endregion
    }

    internal class LiveReadItemCommand : MiCommandDefinition<ItemValueResponseMessage>
    {
        #region Constructors

        public LiveReadItemCommand(int itemNumber)
        {
            ItemNumber = itemNumber;
            var itemString = itemNumber.ToString().PadLeft(3, Convert.ToChar("0"));
            Command = BuildCommand($"{CommandPrefix}{ControlCharacters.STX}{itemString}");
        }

        #endregion

        #region Properties

        public int ItemNumber { get; }

        public override ResponseProcessor<ItemValueResponseMessage> ResponseProcessor
            => ResponseProcessors.ItemValue(ItemNumber);

        #endregion

        #region Fields

        private const string CommandPrefix = "LR";

        #endregion
    }

    internal class MiCommandDefinition<TResponse> : CommandDefinition<TResponse>
            where TResponse : ResponseMessage
    {
        #region Constructors

        public MiCommandDefinition(char controlChar, ResponseProcessor<TResponse> processor = null)
        {
            Command = new string(new[] { controlChar });
            ResponseProcessor = processor;
        }

        public MiCommandDefinition(string body, ResponseProcessor<TResponse> processor)
        {
            Command = BuildCommand(body);
            ResponseProcessor = processor;
        }

        #endregion

        #region Properties

        public override ResponseProcessor<TResponse> ResponseProcessor { get; }

        #endregion

        protected MiCommandDefinition()
        {
        }

        protected string BuildCommand(string body)
        {
            if (!body.Contains(ControlCharacters.ETX))
                body = string.Concat(body, ControlCharacters.ETX);

            var crc = CrcChecksum.Calculate(body);
            return string.Concat(ControlCharacters.SOH, body, crc, ControlCharacters.EOT);
        }
    }

    internal class ReadGroupCommand : MiCommandDefinition<ItemGroupResponseMessage>
    {
        #region Constructors

        /// <summary>
        /// Builds the RG command for 1 to 15 item numbers An exception is thrown is there are more
        /// than 15 items
        /// </summary>
        /// <param name="itemNumbers">Can only be 15 items maximum</param>
        /// <returns></returns>
        public ReadGroupCommand(IEnumerable<int> itemNumbers)
        {
            ItemNumbers = itemNumbers as int[] ?? itemNumbers.ToArray();
            if (ItemNumbers.Count() > 15) throw new ArgumentOutOfRangeException(nameof(itemNumbers));

            var itemsString = JoinItemValues(ItemNumbers);
            var cmd = $"{CommandPrefix}{ControlCharacters.STX}{itemsString}";
            Command = BuildCommand(cmd);
        }

        #endregion

        #region Properties

        public IEnumerable<int> ItemNumbers { get; }

        public override ResponseProcessor<ItemGroupResponseMessage> ResponseProcessor
            => ResponseProcessors.ItemGroup(ItemNumbers);

        #endregion

        #region Fields

        private const string CommandPrefix = "RG";

        #endregion

        #region Methods

        private static string JoinItemValues(IEnumerable<int> items)
        {
            var itemsArray = items.Select(x => x.ToString().PadLeft(3, Convert.ToChar("0"))).ToArray();
            return string.Join(",", itemsArray);
        }

        #endregion
    }

    internal class ReadItemCommand : MiCommandDefinition<ItemValueResponseMessage>
    {
        #region Constructors

        public ReadItemCommand(int itemNumber)
        {
            ItemNumber = itemNumber;
            var itemString = itemNumber.ToString().PadLeft(3, Convert.ToChar("0"));
            Command = BuildCommand($"{CommandPrefix}{ControlCharacters.STX}{itemString}");
        }

        #endregion

        #region Properties

        public int ItemNumber { get; }

        public override ResponseProcessor<ItemValueResponseMessage> ResponseProcessor
            => ResponseProcessors.ItemValue(ItemNumber);

        #endregion

        #region Fields

        private const string CommandPrefix = "RD";

        #endregion
    }

    internal class WriteItemCommand : MiCommandDefinition<StatusResponseMessage>
    {
        #region Constructors

        public WriteItemCommand(int number, string value, string accessCode)
        {
            Number = number;
            Value = value;

            var numberString = Number.ToString().PadLeft(3, Convert.ToChar("0"));
            var valueString = Value.PadLeft(8, Convert.ToChar("0"));
            Command = $"{CommandPrefix},{accessCode}{ControlCharacters.STX}{numberString},{valueString}";
            Command = BuildCommand(Command);
        }

        #endregion

        #region Properties

        public int Number { get; }

        public override ResponseProcessor<StatusResponseMessage> ResponseProcessor => ResponseProcessors.ResponseCode;

        public string Value { get; }

        #endregion

        #region Fields

        private const string CommandPrefix = "WD";

        #endregion
    }
}