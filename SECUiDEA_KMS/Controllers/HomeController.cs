using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SECUiDEA_KMS.Models;
using SECUiDEA_KMS.Models.ClientServers;
using SECUiDEA_KMS.Models.KeyRequests;
using SECUiDEA_KMS.Services;

namespace SECUiDEA_KMS.Controllers
{
    [Localhostonly]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ClientService _clientService;
        private readonly KeyService _keyService;

        public HomeController(
            ILogger<HomeController> logger,
            ClientService clientService,
            KeyService keyService)
        {
            _logger = logger;
            _clientService = clientService;
            _keyService = keyService;
        }

        public IActionResult Index()
        {
            return RedirectToAction(nameof(Clients));
        }

        public IActionResult Privacy()
        {
            return View();
        }

        /// <summary>
        /// 클라이언트 리스트 페이지 (페이징)
        /// </summary>
        public async Task<IActionResult> Clients(int pageNumber = 1, int pageSize = 10)
        {
            var response = await _clientService.GetClientListAsync(pageNumber, pageSize);

            if (!response.IsSuccess)
            {
                TempData["ErrorMessage"] = response.ErrorMessage;
                return View(new ClientListDTO { PageNumber = pageNumber, PageSize = pageSize });
            }

            return View(response.Data);
        }

        /// <summary>
        /// 클라이언트 추가 페이지 (GET)
        /// </summary>
        [HttpGet]
        public IActionResult AddClient()
        {
            return View(new RegisterClientDTO { IPValidationMode = "Strict" });
        }

        /// <summary>
        /// 클라이언트 등록 처리 (POST)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddClient(RegisterClientDTO request)
        {
            if (!ModelState.IsValid)
            {
                return View(request);
            }

            // 등록자 정보 (실제 환경에서는 인증된 사용자 정보 사용)
            var createdBy = User.Identity?.Name ?? "Admin";

            var response = await _clientService.RegisterClientAsync(request, createdBy);

            if (!response.IsSuccess)
            {
                ModelState.AddModelError(string.Empty, response.ErrorMessage);
                return View(request);
            }

            TempData["SuccessMessage"] = "클라이언트가 성공적으로 등록되었습니다.";
            TempData["ClientGuid"] = response.Data?.ClientGuid.ToString();
            return RedirectToAction(nameof(ClientDetail), new { guid = response.Data?.ClientGuid });
        }

        /// <summary>
        /// 클라이언트 상세 페이지
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ClientDetail(Guid guid)
        {
            var response = await _clientService.GetClientInfoAsync(guid);

            if (!response.IsSuccess || response.Data == null)
            {
                TempData["ErrorMessage"] = response.ErrorMessage;
                return RedirectToAction(nameof(Clients));
            }

            return View(response.Data);
        }

        /// <summary>
        /// UI에서 키 생성 (관리자용)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateKey(KeyGenerationReqDTO request)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "입력 값이 유효하지 않습니다.";
                return RedirectToAction(nameof(ClientDetail), new { guid = request.ClientGuid });
            }

            // 자동 회전 검증
            if (request.IsAutoRotation)
            {
                if (!request.ExpirationDays.HasValue || request.ExpirationDays.Value <= 0)
                {
                    TempData["ErrorMessage"] = "자동 회전 키는 만료 일수를 입력해야 합니다.";
                    return RedirectToAction(nameof(ClientDetail), new { guid = request.ClientGuid });
                }
                if (!request.RotationScheduleDays.HasValue || request.RotationScheduleDays.Value <= 0)
                {
                    TempData["ErrorMessage"] = "자동 회전 키는 회전 스케줄을 입력해야 합니다.";
                    return RedirectToAction(nameof(ClientDetail), new { guid = request.ClientGuid });
                }
            }

            var response = await _keyService.GenerateKeyAsync(request);

            if (!response.IsSuccess)
            {
                TempData["ErrorMessage"] = response.ErrorMessage;
            }
            else
            {
                TempData["SuccessMessage"] = "키가 성공적으로 생성되었습니다.";
            }

            return RedirectToAction(nameof(ClientDetail), new { guid = request.ClientGuid });
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
