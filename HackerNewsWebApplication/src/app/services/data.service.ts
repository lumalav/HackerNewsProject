import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs/Observable';
import 'rxjs/add/observable/forkJoin';
import { _throw } from 'rxjs/observable/throw';
import { ConfigService } from './config.service';
import 'rxjs/add/operator/catch';

@Injectable()
export class DataService {

  constructor(private _http: HttpClient,
    private config: ConfigService) { }

  loadNews(params: any): Observable<any> {
    return this._http
      .get(this.config.WEB_API_BASE_URL + this.config._api_urls.get.load + '',
      {
        params: this.toHttpParams(params)
      })
      .catch(this.handleError);
  }

  getTopNews(params: any): Observable<any> {
    console.log('performing query: ', params);
    return this._http
      .get(this.config.WEB_API_BASE_URL + this.config._api_urls.get.fetch + '',
      {
        params: this.toHttpParams(params)
      })
      .catch(this.handleError);
  }

  private handleError(error: any) {
    if (error.status === 400) {
      return _throw(error.error || error);
    } else if (error.status === 0) {
      return _throw(error.error || error);
    } else if (error.status === 401) {
      return _throw(error.error || error);
    } else {
      return _throw(error.error.exceptionMessage || error.message);
    }
  }

  private toHttpParams(obj: Object): HttpParams {
    return Object.getOwnPropertyNames(obj)
      .reduce((p, key) => p.set(key, encodeURIComponent(obj[key])), new HttpParams());
  }
}
