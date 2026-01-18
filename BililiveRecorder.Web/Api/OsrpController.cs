using System;
using System.Linq;
using System.Runtime.InteropServices;
using BililiveRecorder.Core;
using BililiveRecorder.Web.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BililiveRecorder.Web.Api
{
    /// <summary>
    /// OSRP (Open Stream Recording Protocol) v1 API 控制器
    /// </summary>
    [ApiController, Route("osrp/v1")]
    public sealed class OsrpController : ControllerBase
    {
        private const string OsrpVersion = "0.1.0";

        private readonly IRecorder recorder;

        public OsrpController(IRecorder recorder)
        {
            this.recorder = recorder ?? throw new ArgumentNullException(nameof(recorder));
        }

        #region Service Info & Capabilities

        /// <summary>
        /// 获取服务信息
        /// </summary>
        [HttpGet("info")]
        public OsrpServiceInfo GetInfo()
        {
            return new OsrpServiceInfo
            {
                Name = "BililiveRecorder",
                Version = GitVersionInformation.SemVer,
                OsrpVersion = OsrpVersion,
                Capabilities = new[] { "tasks", "streams", "config", "bilibili" },
                Runtime = RuntimeInformation.FrameworkDescription,
                OS = RuntimeInformation.OSDescription,
                Arch = RuntimeInformation.OSArchitecture.ToString()
            };
        }

        /// <summary>
        /// 获取能力声明
        /// </summary>
        [HttpGet("capabilities")]
        public OsrpCapabilities GetCapabilities()
        {
            return new OsrpCapabilities
            {
                StreamUrlResolve = false, // BililiveRecorder 只支持 bilibili
                StreamProbe = false,
                MultiQualityProbe = false,
                Mp4Convert = false,
                SegmentRecording = true,
                Webhook = true,
                Platforms = new[] { "bilibili" }
            };
        }

        #endregion

        #region Task Management

        /// <summary>
        /// 获取所有任务
        /// </summary>
        [HttpGet("tasks")]
        public OsrpTaskListResponse GetTasks()
        {
            var tasks = this.recorder.Rooms.Select(room => ConvertRoomToTask(room)).ToArray();
            return new OsrpTaskListResponse
            {
                Tasks = tasks,
                Total = tasks.Length
            };
        }

        /// <summary>
        /// 获取单个任务
        /// </summary>
        [HttpGet("tasks/{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OsrpError), StatusCodes.Status404NotFound)]
        public ActionResult<OsrpTaskInfo> GetTask(string id)
        {
            var room = FindRoom(id);
            if (room is null)
                return NotFound(new OsrpError { Code = "TASK_NOT_FOUND", Message = "任务不存在" });

            return ConvertRoomToTask(room);
        }

        /// <summary>
        /// 添加任务
        /// </summary>
        [HttpPost("tasks")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(OsrpError), StatusCodes.Status400BadRequest)]
        public ActionResult<OsrpTaskInfo> AddTask([FromBody] OsrpAddTaskRequest request)
        {
            if (request is null || request.RoomId <= 0)
                return BadRequest(new OsrpError { Code = "INVALID_REQUEST", Message = "请求格式错误或房间号无效" });

            var existingRoom = this.recorder.Rooms.FirstOrDefault(x => x.RoomConfig.RoomId == request.RoomId);
            IRoom room;
            if (existingRoom is not null)
            {
                room = existingRoom;
            }
            else
            {
                room = this.recorder.AddRoom(request.RoomId, request.AutoStart);
            }

            return StatusCode(201, ConvertRoomToTask(room));
        }

        /// <summary>
        /// 删除任务
        /// </summary>
        [HttpDelete("tasks/{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OsrpError), StatusCodes.Status404NotFound)]
        public ActionResult<OsrpDeleteResponse> DeleteTask(string id)
        {
            var room = FindRoom(id);
            if (room is null)
                return NotFound(new OsrpError { Code = "TASK_NOT_FOUND", Message = "任务不存在" });

            this.recorder.RemoveRoom(room);
            return new OsrpDeleteResponse { Deleted = true };
        }

        /// <summary>
        /// 任务操作
        /// </summary>
        [HttpPost("tasks/{id}/actions")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OsrpError), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(OsrpError), StatusCodes.Status404NotFound)]
        public ActionResult<OsrpActionResponse> TaskAction(string id, [FromBody] OsrpActionRequest request)
        {
            var room = FindRoom(id);
            if (room is null)
                return NotFound(new OsrpError { Code = "TASK_NOT_FOUND", Message = "任务不存在" });

            switch (request?.Action?.ToLowerInvariant())
            {
                case "start":
                    room.StartRecord();
                    break;
                case "stop":
                    room.StopRecord();
                    break;
                case "cut":
                case "split":
                    room.SplitOutput();
                    break;
                default:
                    return BadRequest(new OsrpError { Code = "INVALID_ACTION", Message = $"不支持的操作: {request?.Action}" });
            }

            return new OsrpActionResponse { Action = request!.Action!, Result = "ok" };
        }

        #endregion

        #region Helper Methods

        private IRoom? FindRoom(string id)
        {
            // 尝试按 ObjectId 查找
            if (Guid.TryParse(id, out var guid))
            {
                var byGuid = this.recorder.Rooms.FirstOrDefault(x => x.ObjectId == guid);
                if (byGuid is not null) return byGuid;
            }

            // 尝试按房间号查找
            if (int.TryParse(id, out var roomId))
            {
                return this.recorder.Rooms.FirstOrDefault(x => x.ShortId == roomId || x.RoomConfig.RoomId == roomId);
            }

            return null;
        }

        private static OsrpTaskInfo ConvertRoomToTask(IRoom room)
        {
            var status = room.Recording ? "recording" : (room.Streaming ? "live" : "waiting");

            return new OsrpTaskInfo
            {
                Id = room.ObjectId.ToString(),
                Platform = "bilibili",
                StreamId = room.RoomConfig.RoomId.ToString(),
                RoomId = room.RoomConfig.RoomId,
                ShortId = room.ShortId,
                Url = $"https://live.bilibili.com/{room.RoomConfig.RoomId}",
                HostName = room.Name,
                RoomName = room.Title,
                Status = status,
                IsLive = room.Streaming,
                IsRecording = room.Recording,
                AutoRecord = room.RoomConfig.AutoRecord
            };
        }

        #endregion
    }

    #region DTOs

    public class OsrpServiceInfo
    {
        public string Name { get; set; } = "";
        public string Version { get; set; } = "";
        public string OsrpVersion { get; set; } = "";
        public string[] Capabilities { get; set; } = Array.Empty<string>();
        public string Runtime { get; set; } = "";
        public string OS { get; set; } = "";
        public string Arch { get; set; } = "";
    }

    public class OsrpCapabilities
    {
        public bool StreamUrlResolve { get; set; }
        public bool StreamProbe { get; set; }
        public bool MultiQualityProbe { get; set; }
        public bool Mp4Convert { get; set; }
        public bool SegmentRecording { get; set; }
        public bool Webhook { get; set; }
        public string[] Platforms { get; set; } = Array.Empty<string>();
    }

    public class OsrpTaskListResponse
    {
        public OsrpTaskInfo[] Tasks { get; set; } = Array.Empty<OsrpTaskInfo>();
        public int Total { get; set; }
    }

    public class OsrpTaskInfo
    {
        public string Id { get; set; } = "";
        public string Platform { get; set; } = "";
        public string StreamId { get; set; } = "";
        public int RoomId { get; set; }
        public int ShortId { get; set; }
        public string Url { get; set; } = "";
        public string HostName { get; set; } = "";
        public string RoomName { get; set; } = "";
        public string Status { get; set; } = "";
        public bool IsLive { get; set; }
        public bool IsRecording { get; set; }
        public bool AutoRecord { get; set; }
    }

    public class OsrpAddTaskRequest
    {
        public int RoomId { get; set; }
        public bool AutoStart { get; set; }
    }

    public class OsrpDeleteResponse
    {
        public bool Deleted { get; set; }
    }

    public class OsrpActionRequest
    {
        public string? Action { get; set; }
    }

    public class OsrpActionResponse
    {
        public string Action { get; set; } = "";
        public string Result { get; set; } = "";
    }

    public class OsrpError
    {
        public string Code { get; set; } = "";
        public string Message { get; set; } = "";
    }

    #endregion
}
