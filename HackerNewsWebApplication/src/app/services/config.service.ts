import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';

@Injectable()
export class ConfigService {

  private config: any;
  private _pageSize: number;

  get pageSize(): number {
    const pageSize = localStorage.getItem('pageSize');

    if (!pageSize) {
      localStorage.setItem('pageSize', '10');
      this._pageSize = 10;
    } else {
      this._pageSize = Number(pageSize);
    }

    return this._pageSize;
  }

  set pageSize(pageSize: number) {
    this._pageSize = pageSize;
    localStorage.setItem('pageSize', this._pageSize.toString());
  }

  public _api_urls = {
    get: {
      fetch: 'fetch',
      load: 'load'
    }
  };

  constructor(private _http: HttpClient) { }

  loadConfiguration() {
    return new Promise((resolve, reject) => {
      this._http.get('assets/app-config.json').subscribe(data => {
        console.log('configuration loaded');
        this.config = data;
        resolve(true);
      }, error => reject(error));
    });
  }

  get APP_VERSION() {
    return this.config.appDetails.version;
  }

  get APP_NAME() {
    return this.config.appDetails.appName;
  }

  get WEB_API_BASE_URL() {
    return this.config.webApi.baseUrl + '/api/hackernews/';
  }

  get PAGE_SIZE_OPTIONS() {
    return this.config.settings.pageSizeOptions;
  }
}
