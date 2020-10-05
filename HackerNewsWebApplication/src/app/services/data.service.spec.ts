import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { DataService } from './data.service';
import { ConfigService } from './config.service';

describe('DataService', () => {
  beforeEach(() => TestBed.configureTestingModule({
    imports: [HttpClientTestingModule],
    providers: [DataService, ConfigService]
  }));

   it('should be created', () => {
    const service: DataService = TestBed.get(DataService);
    expect(service).toBeTruthy();
   });

   it('should have getData function', () => {
    const service: DataService = TestBed.get(DataService);
    expect(service.loadNews).toBeTruthy();
   });
});
