﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TelegramBotFramework.Core.Interfaces;
using TelegramBotFramework.Core.SQLiteDb;
using TgBotFramework.Core.Interfaces;

namespace TelegramBotFramework.Core.Objects
{

    [AttributeUsage(AttributeTargets.Class)]
    public class TelegramBotModule : Attribute
    {
        public string Author { get; set; }
        public string Name { get; set; }
        public string Version { get; set; }
        public bool IsModuleActive { get; set; }
    }
    public abstract class TelegramBotModuleBase<T> : ITelegramBotModule where T : ITelegramBotWrapper
    {
        protected T BotWrapper;

        public TelegramBotModuleBase()
        {

        }
        public TelegramBotModuleBase(T wrapper)
        {
            BotWrapper = wrapper;
        }
        private readonly List<string> Questions = new List<string>();
        protected void SetQuestions(List<string> questions)
        {
            Questions.Clear();
            Questions.AddRange(questions);
        }

        protected virtual CommandResponse SendQuestion(long userId)
        {
            if (!UsersWaitingAnswers.ContainsKey(userId))
            {
                var queue = new Queue<string>();
                foreach (var q in Questions)
                {
                    queue.Enqueue(q);
                }
                UsersWaitingAnswers.TryAdd(userId, queue);
            }
            else
            {
                return new CommandResponse("");
            }
            BotWrapper.AnswerHandling = true;
            return new CommandResponse($"Enter value of `{UsersWaitingAnswers[userId].Peek()}`", parseMode: ParseMode.Markdown);
        }
        protected virtual CommandResponse GetAnswer(long userId)
        {
            if (UsersWaitingAnswers[userId].Peek() == null)
            {
                return new CommandResponse("ok, thx");
            }
            else
            {
                UsersWaitingAnswers[userId].Dequeue();
                return SendQuestion(userId);
            }
        }
    }
    //public abstract class TelegramBotModuleBase : ITelegramBotModule
    //{
    //    protected TelegramBotWrapper BotWrapper;
    //    public TelegramBotModuleBase()
    //    {

    //    }
    //    public TelegramBotModuleBase(TelegramBotWrapper wrapper)
    //    {
    //        BotWrapper = wrapper;
    //    }

    //}

    [AttributeUsage(AttributeTargets.Method)]
    public class ChatCommand : Attribute
    {
        /// <summary>
        /// What triggers the command? starts with ! or /
        /// </summary>
        public string[] Triggers { get; set; }

        /// <summary>
        /// Only bot admins can use (moderators)
        /// </summary>
        public bool BotAdminOnly { get; set; } = false;
        /// <summary>
        /// Only group admins can use this
        /// </summary>
        public bool GroupAdminOnly { get; set; } = false;
        /// <summary>
        /// Only developers / bot owner (you) can use this
        /// </summary>
        public bool DevOnly { get; set; } = false;
        /// <summary>
        /// Allows the command to show inline for admins
        /// </summary>
        public bool AllowInlineAdmin { get; set; } = false;
        /// <summary>
        /// Command can only be used in group
        /// </summary>
        public bool InGroupOnly { get; set; } = false;
        /// <summary>
        /// Command can only be used in Private (private info, or LARGE messages / spam
        /// </summary>
        public bool InPrivateOnly { get; set; } = false;
        /// <summary>
        /// Sets the help text for this command
        /// </summary>
        public string HelpText { get; set; }
        /// <summary>
        /// Does not run the command for inline queries (useful for things like setloc)
        /// </summary>
        public bool HideFromInline { get; set; } = false;
        /// <summary>
        /// Makes it so the command can ONLY be found inline if they type the trigger - keeps API limited commands from being run nonstop
        /// </summary>
        public bool DontSearchInline { get; set; } = false;
        /// <summary>
        /// for help text, tells what parameters can be used
        /// </summary>
        public string[] Parameters { get; set; } = new string[0];
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class CallbackCommand : Attribute
    {
        /// <summary>
        /// What triggers the command? starts with ! or /
        /// </summary>
        public string Trigger { get; set; }
        /// <summary>
        /// Only bot admins can use (moderators)
        /// </summary>
        public bool BotAdminOnly { get; set; } = false;
        /// <summary>
        /// Only group admins can use this
        /// </summary>
        public bool GroupAdminOnly { get; set; } = false;
        /// <summary>
        /// Only developers / bot owner (you) can use this
        /// </summary>
        public bool DevOnly { get; set; } = false;
        /// <summary>
        /// Sets the help text for this command
        /// </summary>
        public string HelpText { get; set; }

    }

    public class CommandResponse
    {
        /// <summary>
        /// The text to send
        /// </summary>
        public string Text { get; set; }
        /// <summary>
        /// Where to reply.  Private = in PM, Public = The chat the message is from
        /// </summary>
        public ResponseLevel Level { get; set; }
        public Menu Menu { get; set; }
        public ParseMode ParseMode { get; set; }
        public string ImageUrl { get; set; }
        public string ImageTitle { get; set; }
        public string ImageCaption { get; set; }
        public string ImageDescription { get; set; }
        /// <summary>
        /// Sends a response through the bot
        /// </summary>
        /// <param name="msg">The text to send</param>
        /// <param name="level">Where to reply.  Private = in PM, Public = The chat the message is from</param>
        /// <param name="replyMarkup">Reply markup.  Optional</param>
        /// <param name="parseMode">How the text should be parsed</param>

        public CommandResponse(string msg, ResponseLevel level = ResponseLevel.Public, Menu menu = null, ParseMode parseMode = ParseMode.Default, string imgUrl = null)
        {
            Text = msg;
            Level = level;
            Menu = menu;
            ParseMode = parseMode;
            ImageUrl = imgUrl;
        }
    }

    public class CommandEventArgs
    {
        public TelegramBotDbContext DatabaseInstance { get; set; }
        public TelegramBotUser SourceUser { get; set; }
        public string Parameters { get; set; }
        public string Target { get; set; } //groupid, userid
        public ModuleMessenger Messenger { get; set; }
        public TelegramBotClient Bot { get; set; }
        public Message Message { get; set; }
    }

    public class CallbackEventArgs
    {
        public TelegramBotDbContext DatabaseInstance { get; set; }
        public TelegramBotUser SourceUser { get; set; }
        public string Parameters { get; set; }
        public string Target { get; set; } //groupid, userid
        public ModuleMessenger Messenger { get; set; }
        public TelegramBotClient Bot { get; set; }
        public CallbackQuery Query { get; set; }
    }

    public delegate void MessageSentEventHandler(object sender, MessageSentEventArgs e);

    public class ModuleMessenger
    {
        public event EventHandler MessageSent;

        protected virtual void OnMessageSent(MessageSentEventArgs e)
        {
            EventHandler handler = MessageSent;
            handler?.Invoke(this, e);
        }

        public void SendMessage(MessageSentEventArgs args)
        {
            OnMessageSent(args);
        }
    }

    public class MessageSentEventArgs : EventArgs
    {
        public string Target { get; set; }
        public CommandResponse Response { get; set; }
    }

    /// <summary>
    /// Forces a response level
    /// </summary>
    public enum ResponseLevel
    {
        Public, Private
    }

    //custom buttons
    public class InlineButton
    {
        /// <summary>
        /// The button text
        /// </summary>
        public string Text { get; set; }
        /// <summary>
        /// What trigger to associate this button with.  Make sure you create a CallbackCommand with the trigger set (Optional)
        /// </summary>
        public string Trigger { get; set; }
        /// <summary>
        /// Any extra data you want to include, not visible to the user (Optional)
        /// </summary>
        public string ExtraData { get; set; }
        /// <summary>
        /// Have this button link to a chat or website. (Optional)
        /// </summary>
        public string Url { get; set; }

        public InlineButton(string text, string trigger = "", string extraData = "", string url = "")
        {
            Url = url;
            Text = text;
            Trigger = trigger;
            ExtraData = extraData;
        }
    }

    public class Menu
    {
        /// <summary>
        /// The buttons you want in your menu
        /// </summary>
        public List<InlineButton> Buttons { get; set; }
        /// <summary>
        /// How many columns.  Defaults to 1.
        /// </summary>
        public int Columns { get; set; }

        public Menu(int col = 1, List<InlineButton> buttons = null)
        {
            Buttons = buttons ?? new List<InlineButton>();
            Columns = Math.Max(col, 1);
        }
    }
}
