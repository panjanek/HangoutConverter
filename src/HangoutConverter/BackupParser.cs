using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace HangoutConverter
{
    public static class BackupParser
    {
        public static ChatHistory ExtractConversation(string backupFile, string participant1, string participant2)
        {
            var history = new ChatHistory();
            string[] split = backupFile.Split('\\');
            string baseDir = string.Join("\\", split.Take(split.Length - 1));
            string tmpDir = Path.Combine(baseDir, "tmp");
            if (!Directory.Exists(tmpDir))
            {
                Console.WriteLine($"Creating temporary directory {tmpDir}");
                Directory.CreateDirectory(tmpDir);
            }
            else
            {
                Console.WriteLine($"Using temporary directory {tmpDir}");
            }

            Console.WriteLine($"Reading JSON from {backupFile}");
            using (StreamReader sr = new StreamReader(backupFile))
            {
                using (JsonReader reader = new JsonTextReader(sr))
                {
                    var rootJson = JObject.ReadFrom(reader);
                    var conversationsJson = rootJson["conversations"];

                    int count = 0;
                    Console.Write($"Extracting chat messages for {participant1} and {participant2}");
                    foreach (var conversationJson in conversationsJson)
                    {
                        var conversationData = conversationJson["conversation"]["conversation"];
                        var participantsData = conversationData["participant_data"];
                        if (participantsData.Count() == 2)
                        {
                            var p1 = participantsData.FirstOrDefault(p => p["fallback_name"] != null && p["fallback_name"].ToString() == participant1);
                            var p2 = participantsData.FirstOrDefault(p => p["fallback_name"] != null && p["fallback_name"].ToString() == participant2);
                            if (p1 != null && p2 != null)
                            {
                                string participant1id = p1["id"]["gaia_id"].ToString();
                                string participant2id = p2["id"]["gaia_id"].ToString();
                                history.Participants[participant1id] = participant1;
                                history.Participants[participant2id] = participant2;

                                var eventsJson = conversationJson["events"];
                                foreach (var eventJson in eventsJson)
                                {
                                    string senderId = eventJson["sender_id"]["gaia_id"].ToString();
                                    string timestamp = eventJson["timestamp"].ToString();
                                    long unixTime = long.Parse(timestamp);
                                    var time = DateTimeOffset.FromUnixTimeSeconds(unixTime / 1000000).AddTicks((unixTime % 1000000) * 10).ToLocalTime();
                                    var messageJson = eventJson["chat_message"];
                                    if (messageJson != null)
                                    {
                                        var messageContentJson = messageJson["message_content"];
                                        if (messageContentJson != null)
                                        {
                                            var segmentsJson = messageContentJson["segment"];
                                            if (segmentsJson != null)
                                            {
                                                foreach (var segmentJson in segmentsJson)
                                                {
                                                    count++;
                                                    if (count % 100 == 0)
                                                    {
                                                        Console.Write(".");
                                                    }

                                                    string type = segmentJson["type"].ToString();
                                                    if (type == "TEXT")
                                                    {
                                                        string text = segmentJson["text"].ToString();
                                                        if (!string.IsNullOrWhiteSpace(text))
                                                        {
                                                            ChatItem item = new ChatItem();
                                                            item.Time = time;
                                                            item.ParticipantId = senderId;
                                                            item.Text = text.ReplaceSpecialChars();
                                                            item.Type = ChatItemType.Text;
                                                            history.Conversation.Add(item);
                                                        }
                                                    }
                                                    else if (type == "LINK")
                                                    {
                                                        string text = segmentJson["text"].ToString();
                                                        string url = segmentJson["link_data"]["link_target"].ToString();
                                                        ChatItem item = new ChatItem();
                                                        item.Time = time;
                                                        item.ParticipantId = senderId;
                                                        item.Text = text;
                                                        item.Url = url;
                                                        item.Type = ChatItemType.Link;
                                                        history.Conversation.Add(item);
                                                    }
                                                }
                                            }

                                            var attachmentsJson = messageContentJson["attachment"];
                                            if (attachmentsJson != null)
                                            {
                                                foreach (var attachmentJson in attachmentsJson)
                                                {
                                                    var embedItemJson = attachmentJson["embed_item"];
                                                    if (embedItemJson != null)
                                                    {
                                                        var plusPhotoJson = embedItemJson["plus_photo"];
                                                        if (plusPhotoJson != null)
                                                        {
                                                            string imageUrl = embedItemJson["plus_photo"]["url"].ToString();
                                                            string mediaType = embedItemJson["plus_photo"]["media_type"].ToString();

                                                            if (mediaType == "PHOTO" || mediaType == "VIDEO")
                                                            {
                                                                string localPath = null;
                                                                string imageFile = imageUrl.Split('/').LastOrDefault();
                                                                if (!imageFile.Contains('.'))
                                                                {
                                                                    imageFile = imageFile + ".jpg";
                                                                }

                                                                localPath = Path.Combine(baseDir, imageFile);
                                                                localPath = localPath.Replace("%253F", "_").Replace("%253D", "=");
                                                                if (!File.Exists(localPath))
                                                                {
                                                                    localPath = null;
                                                                }

                                                                if (mediaType == "PHOTO")
                                                                {
                                                                    try
                                                                    {
                                                                        Console.Write("i");
                                                                        string guid = Guid.NewGuid().ToString();
                                                                        string fullsizeImagePath = Path.Combine(tmpDir, guid + "_fullsize_" + imageFile);
                                                                        string thumbnailImagePath = Path.Combine(tmpDir, guid + "_thumb_" + imageFile);
                                                                        using (var client = new WebClient())
                                                                        {
                                                                            client.DownloadFile(imageUrl, fullsizeImagePath);
                                                                        }

                                                                        ImageUtil.ResizeImage(fullsizeImagePath, thumbnailImagePath, 350);
                                                                        localPath = thumbnailImagePath;
                                                                    }
                                                                    catch (Exception downloadEx)
                                                                    {

                                                                    }
                                                                }
                                                                else
                                                                {
                                                                    Console.Write("v");
                                                                }

                                                                ChatItem item = new ChatItem();
                                                                item.Time = time;
                                                                item.ParticipantId = senderId;
                                                                item.Url = imageUrl;
                                                                item.Type = mediaType == "PHOTO" ? ChatItemType.Image : ChatItemType.Video;
                                                                item.LocalPath = localPath;
                                                                history.Conversation.Add(item);
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    Console.WriteLine();
                    Console.WriteLine($"Extracted {count} messages");
                }
            }

            history.Conversation = history.Conversation.OrderBy(c => c.Time).ToList();
            return history;
        }
    }
}
